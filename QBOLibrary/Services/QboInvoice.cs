using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // IInvoiceQuery / IInvoiceAdd / IInvoiceMod / ITxnVoid / ITxnDel
    public class QboInvoice
    {
        private const string Entity = "Invoice";
        private const string Path = "invoice";

        private readonly QboHttp _http;

        public QboInvoice(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboInvoiceModel>>> QuerySinceAsync(DateTime since)
        {
            return QboEntityHelper.QueryPagedAsync<QboInvoiceModel>(
                _http, Entity, "MetaData.LastUpdatedTime > " + QboQuery.Date(since),
                "MetaData.LastUpdatedTime");
        }

        // Full invoice list for the post-conversion remap - paged, ordered by Id
        public Task<QboResultModel<List<QboInvoiceModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboInvoiceModel>(_http, Entity, null, "Id");
        }

        public Task<QboResultModel<List<QboInvoiceModel>>> QueryByCustomerAsync(string customerId)
        {
            return QboEntityHelper.QueryPagedAsync<QboInvoiceModel>(
                _http, Entity, "CustomerRef = " + QboQuery.Literal(customerId), "Id", customerId);
        }

        // Replaces QueryUnpaidInvoices - QBO exposes Balance directly
        public Task<QboResultModel<List<QboInvoiceModel>>> QueryUnpaidAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboInvoiceModel>(
                _http, Entity, "Balance > '0'", "Id");
        }

        public Task<QboResultModel<List<QboInvoiceModel>>> QueryUnpaidByCustomerAsync(string customerId)
        {
            string where = "CustomerRef = " + QboQuery.Literal(customerId) + " and Balance > '0'";
            return QboEntityHelper.QueryPagedAsync<QboInvoiceModel>(_http, Entity, where, "Id", customerId);
        }

        // Replaces QueryInvoicebyReferenceNumber
        public Task<QboResultModel<QboInvoiceModel>> QueryByDocNumberAsync(string docNumber)
        {
            return QboEntityHelper.QueryFirstAsync<QboInvoiceModel>(
                _http, Entity, "DocNumber = " + QboQuery.Literal(docNumber), docNumber);
        }

        // Desktop QueryQBNewInvoices - by creation date
        public Task<QboResultModel<List<QboInvoiceModel>>> QueryCreatedBetweenAsync(DateTime from, DateTime to)
        {
            string where = "MetaData.CreateTime >= " + QboQuery.Date(from)
                         + " and MetaData.CreateTime <= " + QboQuery.Date(to.AddDays(1));
            return QboEntityHelper.QueryPagedAsync<QboInvoiceModel>(
                _http, Entity, where, "MetaData.CreateTime");
        }

        // Desktop QueryQBModifiedInvoices - by last modified date
        public Task<QboResultModel<List<QboInvoiceModel>>> QueryModifiedBetweenAsync(DateTime from, DateTime to)
        {
            string where = "MetaData.LastUpdatedTime >= " + QboQuery.Date(from)
                         + " and MetaData.LastUpdatedTime <= " + QboQuery.Date(to.AddDays(1));
            return QboEntityHelper.QueryPagedAsync<QboInvoiceModel>(
                _http, Entity, where, "MetaData.LastUpdatedTime");
        }

        public async Task<QboResultModel<QboInvoiceModel>> GetByIdAsync(string id)
        {
            var response = await _http.GetAsync(Path + "/" + id, Entity, "READ", id).ConfigureAwait(false);
            if (!response.Success)
            {
                return QboResultModel<QboInvoiceModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        // Guarded against double posting - a DocNumber that already exists is
        // returned rather than created a second time.
        public async Task<QboResultModel<QboInvoiceModel>> AddAsync(QboInvoiceModel invoice)
        {
            if (!string.IsNullOrEmpty(invoice.DocNumber))
            {
                QboResultModel<QboInvoiceModel> existing =
                    await QueryByDocNumberAsync(invoice.DocNumber).ConfigureAwait(false);

                if (existing.Success && existing.Data != null)
                {
                    return QboResultModel<QboInvoiceModel>.Fail(409, "DUPLICATE_DOCNUMBER",
                        "Invoice " + invoice.DocNumber + " already exists in QuickBooks Online as Id "
                        + existing.Data.Id + ".");
                }
            }

            invoice.Id = null;
            invoice.SyncToken = null;

            var response = await _http
                .PostAsync(Path, invoice, Entity, "CREATE", invoice.DocNumber)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboInvoiceModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        // Full update - required when line items change
        public async Task<QboResultModel<QboInvoiceModel>> ModifyAsync(QboInvoiceModel invoice)
        {
            QboResultModel<QboInvoiceModel> current = await GetByIdAsync(invoice.Id).ConfigureAwait(false);
            if (!current.Success)
            {
                return current;
            }
            if (current.Data == null)
            {
                return NotFound(invoice.Id);
            }

            invoice.SyncToken = current.Data.SyncToken;
            invoice.Sparse = null;

            var response = await _http
                .PostAsync(Path, invoice, Entity, "UPDATE", invoice.Id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboInvoiceModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        // Sparse update - only the supplied fields change
        public async Task<QboResultModel<QboInvoiceModel>> ModifySparseAsync(QboInvoiceModel changes)
        {
            QboResultModel<QboInvoiceModel> current = await GetByIdAsync(changes.Id).ConfigureAwait(false);
            if (!current.Success)
            {
                return current;
            }
            if (current.Data == null)
            {
                return NotFound(changes.Id);
            }

            changes.SyncToken = current.Data.SyncToken;
            changes.Sparse = true;

            var response = await _http
                .PostAsync(Path, changes, Entity, "UPDATE", changes.Id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboInvoiceModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        // ITxnVoid - amounts zero out, the transaction stays in the ledger
        public async Task<QboResultModel<QboInvoiceModel>> VoidAsync(string id)
        {
            QboResultModel<QboInvoiceModel> current = await GetByIdAsync(id).ConfigureAwait(false);
            if (!current.Success)
            {
                return current;
            }
            if (current.Data == null)
            {
                return NotFound(id);
            }

            var body = new { Id = id, SyncToken = current.Data.SyncToken };

            var response = await _http
                .PostAsync(Path + "?operation=void", body, Entity, "VOID", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboInvoiceModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        // ITxnDel - linked transactions must be unlinked first or QBO refuses
        public async Task<QboResultModel<bool>> DeleteAsync(string id)
        {
            QboResultModel<QboInvoiceModel> current = await GetByIdAsync(id).ConfigureAwait(false);
            if (!current.Success)
            {
                return QboResultModel<bool>.FromFailure(current);
            }
            if (current.Data == null)
            {
                return QboResultModel<bool>.Fail(404, "NOT_FOUND",
                    "Invoice " + id + " was not found in QuickBooks Online.");
            }
            if (current.Data.LinkedTxn != null && current.Data.LinkedTxn.Count > 0)
            {
                return QboResultModel<bool>.Fail(400, "LINKED_TXN",
                    "Invoice " + id + " has linked transactions and cannot be deleted. Void it instead.");
            }

            var body = new { Id = id, SyncToken = current.Data.SyncToken };

            var response = await _http
                .PostAsync(Path + "?operation=delete", body, Entity, "DELETE", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<bool>.FromFailure(response);
            }
            return QboResultModel<bool>.Ok(true, response.HttpStatus, response.IntuitTid, response.RawResponse);
        }

        private static QboResultModel<QboInvoiceModel> Unwrap(QboResultModel<JObject> response)
        {
            return QboResultModel<QboInvoiceModel>.Ok(
                QboEntityHelper.ReadOne<QboInvoiceModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }

        private static QboResultModel<QboInvoiceModel> NotFound(string id)
        {
            return QboResultModel<QboInvoiceModel>.Fail(404, "NOT_FOUND",
                "Invoice " + id + " was not found in QuickBooks Online.");
        }
    }
}
