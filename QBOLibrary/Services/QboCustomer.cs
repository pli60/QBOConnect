using System.Collections.Generic;
using System.Threading.Tasks;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // ICustomerQuery / ICustomerAdd / ICustomerMod
    public class QboCustomer
    {
        private const string Entity = "Customer";
        private const string Path = "customer";

        private readonly QboHttp _http;

        public QboCustomer(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboCustomerModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboCustomerModel>(
                _http, Entity, null, "Id");
        }

        public Task<QboResultModel<List<QboCustomerModel>>> QueryActiveAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboCustomerModel>(
                _http, Entity, "Active = true", "Id");
        }

        public Task<QboResultModel<List<QboCustomerModel>>> QueryWithBalanceAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboCustomerModel>(
                _http, Entity, "Balance > '0'", "Id");
        }

        public Task<QboResultModel<QboCustomerModel>> QueryByDisplayNameAsync(string displayName)
        {
            return QboEntityHelper.QueryFirstAsync<QboCustomerModel>(
                _http, Entity, "DisplayName = " + QboQuery.Literal(displayName), displayName);
        }

        public async Task<QboResultModel<QboCustomerModel>> GetByIdAsync(string id)
        {
            var response = await _http.GetAsync(Path + "/" + id, Entity, "READ", id).ConfigureAwait(false);
            if (!response.Success)
            {
                return QboResultModel<QboCustomerModel>.FromFailure(response);
            }
            return QboResultModel<QboCustomerModel>.Ok(
                QboEntityHelper.ReadOne<QboCustomerModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }

        // DisplayName is unique across Customer, Vendor, Employee and Other Names.
        // A collision returns error 6240 rather than creating a duplicate.
        public async Task<QboResultModel<QboCustomerModel>> AddAsync(QboCustomerModel customer)
        {
            customer.Id = null;
            customer.SyncToken = null;

            var response = await _http
                .PostAsync(Path, customer, Entity, "CREATE", customer.DisplayName)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboCustomerModel>.FromFailure(response);
            }
            return QboResultModel<QboCustomerModel>.Ok(
                QboEntityHelper.ReadOne<QboCustomerModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }

        // Full update. SyncToken is read immediately before the write so callers
        // never have to store or track it.
        public async Task<QboResultModel<QboCustomerModel>> ModifyAsync(QboCustomerModel customer)
        {
            QboResultModel<QboCustomerModel> current = await GetByIdAsync(customer.Id).ConfigureAwait(false);
            if (!current.Success)
            {
                return current;
            }
            if (current.Data == null)
            {
                return QboResultModel<QboCustomerModel>.Fail(404, "NOT_FOUND",
                    "Customer " + customer.Id + " was not found in QuickBooks Online.");
            }

            customer.SyncToken = current.Data.SyncToken;
            customer.Sparse = null;

            var response = await _http
                .PostAsync(Path, customer, Entity, "UPDATE", customer.Id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboCustomerModel>.FromFailure(response);
            }
            return QboResultModel<QboCustomerModel>.Ok(
                QboEntityHelper.ReadOne<QboCustomerModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }

        // QBO has no delete for name list entities - IListDel becomes deactivation
        public async Task<QboResultModel<QboCustomerModel>> DeactivateAsync(string id)
        {
            QboResultModel<QboCustomerModel> current = await GetByIdAsync(id).ConfigureAwait(false);
            if (!current.Success)
            {
                return current;
            }
            if (current.Data == null)
            {
                return QboResultModel<QboCustomerModel>.Fail(404, "NOT_FOUND",
                    "Customer " + id + " was not found in QuickBooks Online.");
            }

            var body = new QboCustomerModel
            {
                Id = id,
                SyncToken = current.Data.SyncToken,
                Active = false,
                Sparse = true
            };

            var response = await _http
                .PostAsync(Path, body, Entity, "UPDATE", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboCustomerModel>.FromFailure(response);
            }
            return QboResultModel<QboCustomerModel>.Ok(
                QboEntityHelper.ReadOne<QboCustomerModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
