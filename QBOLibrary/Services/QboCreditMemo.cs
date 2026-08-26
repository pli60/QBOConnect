using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // ICreditMemoAdd
    public class QboCreditMemo
    {
        private const string Entity = "CreditMemo";
        private const string Path = "creditmemo";

        private readonly QboHttp _http;

        public QboCreditMemo(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboCreditMemoModel>>> QueryByCustomerAsync(string customerId)
        {
            return QboEntityHelper.QueryPagedAsync<QboCreditMemoModel>(
                _http, Entity, "CustomerRef = " + QboQuery.Literal(customerId), "Id", customerId);
        }

        public Task<QboResultModel<List<QboCreditMemoModel>>> QuerySinceAsync(DateTime since)
        {
            return QboEntityHelper.QueryPagedAsync<QboCreditMemoModel>(
                _http, Entity, "MetaData.LastUpdatedTime > " + QboQuery.Date(since),
                "MetaData.LastUpdatedTime");
        }

        public async Task<QboResultModel<QboCreditMemoModel>> GetByIdAsync(string id)
        {
            var response = await _http.GetAsync(Path + "/" + id, Entity, "READ", id).ConfigureAwait(false);
            if (!response.Success)
            {
                return QboResultModel<QboCreditMemoModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        public async Task<QboResultModel<QboCreditMemoModel>> AddAsync(QboCreditMemoModel creditMemo)
        {
            creditMemo.Id = null;
            creditMemo.SyncToken = null;

            var response = await _http
                .PostAsync(Path, creditMemo, Entity, "CREATE", creditMemo.DocNumber)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboCreditMemoModel>.FromFailure(response);
            }
            return Unwrap(response);
        }

        private static QboResultModel<QboCreditMemoModel> Unwrap(QboResultModel<JObject> response)
        {
            return QboResultModel<QboCreditMemoModel>.Ok(
                QboEntityHelper.ReadOne<QboCreditMemoModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
