using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // Turns LIMS names into QBO Ids. LIMS stores names (FIN_SYS_ITEM_CODE,
    // ClassRef_FullName, SalesTaxCode); QBO requires numeric Ids on every ref.
    public class QboRefResolver
    {
        public const string EntityCustomer = "CUSTOMER";
        public const string EntityItem = "ITEM";
        public const string EntityTerm = "TERM";
        public const string EntityAccount = "ACCOUNT";
        public const string EntityTaxCode = "TAXCODE";
        public const string EntityCustomerType = "CUSTOMERTYPE";

        private readonly QboSession _session;
        private readonly IQboMapStore _mapStore;

        private Dictionary<string, QboItemModel> _items;
        private Dictionary<string, QboTermModel> _terms;
        private Dictionary<string, QboAccountModel> _accounts;
        private Dictionary<string, QboTaxCodeModel> _taxCodes;
        private Dictionary<string, QboCustomerTypeModel> _customerTypes;

        private DateTime _loadedOn = DateTime.MinValue;

        public int CacheMinutes { get; set; } = 60;

        public string LastError { get; private set; }

        public bool IsLoaded => _items != null
                                && _loadedOn.AddMinutes(CacheMinutes) > DateTime.Now;

        // mapStore is optional - pass null to run purely against live QBO data
        public QboRefResolver(QboSession session, IQboMapStore mapStore)
        {
            _session = session;
            _mapStore = mapStore;
        }

        public async Task<QboResultModel<bool>> LoadAsync(bool force = false)
        {
            if (IsLoaded && !force)
            {
                return QboResultModel<bool>.Ok(true);
            }

            var itemResult = await _session.Items.QueryActiveAsync().ConfigureAwait(false);
            if (!itemResult.Success)
            {
                LastError = itemResult.FullError;
                return QboResultModel<bool>.FromFailure(itemResult);
            }

            var termResult = await _session.Terms.QueryActiveAsync().ConfigureAwait(false);
            if (!termResult.Success)
            {
                LastError = termResult.FullError;
                return QboResultModel<bool>.FromFailure(termResult);
            }

            var accountResult = await _session.Accounts.QueryActiveAsync().ConfigureAwait(false);
            if (!accountResult.Success)
            {
                LastError = accountResult.FullError;
                return QboResultModel<bool>.FromFailure(accountResult);
            }

            var taxResult = await _session.TaxCodes.QueryAllAsync().ConfigureAwait(false);
            if (!taxResult.Success)
            {
                LastError = taxResult.FullError;
                return QboResultModel<bool>.FromFailure(taxResult);
            }

            var typeResult = await _session.CustomerTypes.QueryAllAsync().ConfigureAwait(false);
            if (!typeResult.Success)
            {
                LastError = typeResult.FullError;
                return QboResultModel<bool>.FromFailure(typeResult);
            }

            _items = BuildIndex(itemResult.Data, i => i.FullyQualifiedName, i => i.Name);
            _terms = BuildIndex(termResult.Data, t => t.Name, t => t.Name);
            _accounts = BuildIndex(accountResult.Data, a => a.FullyQualifiedName, a => a.Name);
            _taxCodes = BuildIndex(taxResult.Data, c => c.Name, c => c.Name);
            _customerTypes = BuildIndex(typeResult.Data, t => t.Name, t => t.Name);

            _loadedOn = DateTime.Now;
            LastError = null;
            return QboResultModel<bool>.Ok(true);
        }

        // ---- item -------------------------------------------------------

        // Category items cannot be used on transaction lines - QBO returns 2500
        public QboItemModel FindItem(string name)
        {
            if (_items == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            QboItemModel item;
            return _items.TryGetValue(name.Trim(), out item) ? item : null;
        }

        public QboRefModel ItemRef(string name)
        {
            QboItemModel item = FindItem(name);
            if (item == null)
            {
                LastError = "Item '" + name + "' was not found in QuickBooks Online.";
                return null;
            }
            if (string.Equals(item.Type, "Category", StringComparison.OrdinalIgnoreCase))
            {
                LastError = "Item '" + name + "' is a Category in QuickBooks Online and cannot be "
                            + "used on an invoice line. Change its type to Service or Non-inventory.";
                return null;
            }
            return new QboRefModel(item.Id, item.Name);
        }

        // ---- other reference lists --------------------------------------

        public QboRefModel TermRef(string name)
        {
            QboTermModel term = Lookup(_terms, name);
            return term == null ? null : new QboRefModel(term.Id, term.Name);
        }

        public QboRefModel AccountRef(string name)
        {
            QboAccountModel account = Lookup(_accounts, name);
            return account == null ? null : new QboRefModel(account.Id, account.Name);
        }

        // Under Automated Sales Tax pass TAX or NON, not a named rate
        public QboRefModel TaxCodeRef(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new QboRefModel("NON");
            }
            QboTaxCodeModel code = Lookup(_taxCodes, name);
            return code == null ? new QboRefModel("NON") : new QboRefModel(code.Id, code.Name);
        }

        public QboRefModel CustomerTypeRef(string name)
        {
            QboCustomerTypeModel type = Lookup(_customerTypes, name);
            return type == null ? null : new QboRefModel(type.Id, type.Name);
        }

        // ---- customer ---------------------------------------------------

        // Map table first, then a live DisplayName lookup as fallback
        public async Task<string> ResolveCustomerIdAsync(string limsKey, string qbFullName)
        {
            if (_mapStore != null && !string.IsNullOrEmpty(limsKey))
            {
                string mapped = _mapStore.GetQboId(EntityCustomer, limsKey);
                if (!string.IsNullOrEmpty(mapped))
                {
                    return mapped;
                }
            }

            if (string.IsNullOrWhiteSpace(qbFullName))
            {
                return null;
            }

            var result = await _session.Customers
                .QueryByDisplayNameAsync(qbFullName.Trim())
                .ConfigureAwait(false);

            if (!result.Success || result.Data == null)
            {
                LastError = result.Success
                    ? "No QuickBooks Online customer named '" + qbFullName + "'."
                    : result.FullError;
                return null;
            }

            if (_mapStore != null && !string.IsNullOrEmpty(limsKey))
            {
                _mapStore.Upsert(EntityCustomer, limsKey, qbFullName,
                                 result.Data.Id, result.Data.DisplayName, "MATCHED", "QboRefResolver");
            }
            return result.Data.Id;
        }

        // ---- persistence -------------------------------------------------

        // Writes the loaded reference lists into QBO_MAP so LIMS SQL can join on them
        public int PersistReferenceLists(string updatedBy)
        {
            if (_mapStore == null || _items == null)
            {
                return 0;
            }

            int written = 0;

            foreach (QboItemModel item in _items.Values.Distinct())
            {
                string status = string.Equals(item.Type, "Category", StringComparison.OrdinalIgnoreCase)
                    ? "CONFLICT"
                    : "MATCHED";
                _mapStore.Upsert(EntityItem, item.Name, item.FullyQualifiedName,
                                 item.Id, item.Name, status, updatedBy);
                written++;
            }
            foreach (QboTermModel term in _terms.Values.Distinct())
            {
                _mapStore.Upsert(EntityTerm, term.Name, term.Name, term.Id, term.Name, "MATCHED", updatedBy);
                written++;
            }
            foreach (QboTaxCodeModel code in _taxCodes.Values.Distinct())
            {
                _mapStore.Upsert(EntityTaxCode, code.Name, code.Name, code.Id, code.Name, "MATCHED", updatedBy);
                written++;
            }
            foreach (QboAccountModel account in _accounts.Values.Distinct())
            {
                _mapStore.Upsert(EntityAccount, account.Name, account.FullyQualifiedName,
                                 account.Id, account.Name, "MATCHED", updatedBy);
                written++;
            }
            foreach (QboCustomerTypeModel type in _customerTypes.Values.Distinct())
            {
                _mapStore.Upsert(EntityCustomerType, type.Name, type.Name,
                                 type.Id, type.Name, "MATCHED", updatedBy);
                written++;
            }

            return written;
        }

        // ---- helpers -----------------------------------------------------

        // Indexed by both fully qualified and short name so either lookup works
        private static Dictionary<string, T> BuildIndex<T>(List<T> source,
                                                           Func<T, string> primaryKey,
                                                           Func<T, string> fallbackKey)
        {
            var index = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            if (source == null)
            {
                return index;
            }

            foreach (T row in source)
            {
                string key = primaryKey(row);
                if (!string.IsNullOrWhiteSpace(key) && !index.ContainsKey(key.Trim()))
                {
                    index[key.Trim()] = row;
                }

                string alt = fallbackKey(row);
                if (!string.IsNullOrWhiteSpace(alt) && !index.ContainsKey(alt.Trim()))
                {
                    index[alt.Trim()] = row;
                }
            }
            return index;
        }

        private T Lookup<T>(Dictionary<string, T> index, string name) where T : class
        {
            if (index == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            T found;
            return index.TryGetValue(name.Trim(), out found) ? found : null;
        }
    }
}
