using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // IReceivePaymentQuery / IReceivePaymentAdd
    public class QboPayment
    {
        private const string Entity = "Payment";
        private const string Path = "payment";

        private readonly QboHttp _http;

        public QboPayment(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboPaymentModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboPaymentModel>(_http, Entity, null, "Id");
        }

        public Task<QboResultModel<List<QboPaymentModel>>> QuerySinceAsync(DateTime since)
        {
            return QboEntityHelper.QueryPagedAsync<QboPaymentModel>(
                _http, Entity, "MetaData.LastUpdatedTime > " + QboQuery.Date(since),
                "MetaData.LastUpdatedTime");
        }

        public Task<QboResultModel<List<QboPaymentModel>>> QueryByCustomerAsync(string customerId)
        {
            return QboEntityHelper.QueryPagedAsync<QboPaymentModel>(
                _http, Entity, "CustomerRef = " + QboQuery.Literal(customerId), "Id", customerId);
        }

        public async Task<QboResultModel<QboPaymentModel>> GetByIdAsync(string id)
        {
            var response = await _http.GetAsync(Path + "/" + id, Entity, "READ", id).ConfigureAwait(false);
            if (!response.Success)
            {
                return QboResultModel<QboPaymentModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        public async Task<QboResultModel<QboPaymentModel>> AddAsync(QboPaymentModel payment)
        {
            payment.Id = null;
            payment.SyncToken = null;

            var response = await _http
                .PostAsync(Path, payment, Entity, "CREATE", payment.PaymentRefNum)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboPaymentModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        public async Task<QboResultModel<QboPaymentModel>> VoidAsync(string id)
        {
            QboResultModel<QboPaymentModel> current = await GetByIdAsync(id).ConfigureAwait(false);
            if (!current.Success)
            {
                return current;
            }
            if (current.Data == null)
            {
                return QboResultModel<QboPaymentModel>.Fail(404, "NOT_FOUND",
                    "Payment " + id + " was not found in QuickBooks Online.");
            }

            var body = new { Id = id, SyncToken = current.Data.SyncToken, sparse = true };

            var response = await _http
                .PostAsync(Path + "?operation=update&include=void", body, Entity, "VOID", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboPaymentModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        // Applies a payment against one or more invoices
        public static QboPaymentModel BuildApplied(string customerId, decimal amount,
                                                   DateTime txnDate, string refNum,
                                                   IDictionary<string, decimal> invoiceIdToAmount)
        {
            List<QboPaymentLineModel> lines = new List<QboPaymentLineModel>();

            if (invoiceIdToAmount != null)
            {
                foreach (KeyValuePair<string, decimal> pair in invoiceIdToAmount)
                {
                    lines.Add(new QboPaymentLineModel
                    {
                        Amount = pair.Value,
                        LinkedTxn = new List<QboLinkedTxnModel>
                        {
                            new QboLinkedTxnModel { TxnId = pair.Key, TxnType = "Invoice" }
                        }
                    });
                }
            }

            return new QboPaymentModel
            {
                CustomerRef = new QboRefModel(customerId),
                TotalAmt = amount,
                TxnDate = txnDate,
                PaymentRefNum = refNum,
                Line = lines.Count > 0 ? lines : null
            };
        }

        private static QboResultModel<QboPaymentModel> Unwrap(QboResultModel<JObject> response)
        {
            return QboResultModel<QboPaymentModel>.Ok(
                QboEntityHelper.ReadOne<QboPaymentModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
