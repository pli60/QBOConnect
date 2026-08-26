using System.Collections.Generic;
using System.Threading.Tasks;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // IAccountQuery
    public class QboAccount
    {
        private const string Entity = "Account";

        private readonly QboHttp _http;

        public QboAccount(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboAccountModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboAccountModel>(_http, Entity);
        }

        public Task<QboResultModel<List<QboAccountModel>>> QueryActiveAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboAccountModel>(_http, Entity, "Active = true");
        }

        public Task<QboResultModel<QboAccountModel>> QueryByNameAsync(string name)
        {
            return QboEntityHelper.QueryFirstAsync<QboAccountModel>(
                _http, Entity, "Name = " + QboQuery.Literal(name), name);
        }

        public async Task<QboResultModel<QboAccountModel>> GetByIdAsync(string id)
        {
            var response = await _http
                .GetAsync("account/" + id, Entity, "READ", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboAccountModel>.FromFailure(response);
            }
            return QboResultModel<QboAccountModel>.Ok(
                QboEntityHelper.ReadOne<QboAccountModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
