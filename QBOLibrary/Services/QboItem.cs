using System.Collections.Generic;
using System.Threading.Tasks;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // resolves FIN_SYS_ITEM_CODE to ItemRef
    public class QboItem
    {
        private const string Entity = "Item";

        private readonly QboHttp _http;

        public QboItem(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboItemModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboItemModel>(_http, Entity);
        }

        public Task<QboResultModel<List<QboItemModel>>> QueryActiveAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboItemModel>(_http, Entity, "Active = true");
        }

        public Task<QboResultModel<QboItemModel>> QueryByNameAsync(string name)
        {
            return QboEntityHelper.QueryFirstAsync<QboItemModel>(
                _http, Entity, "Name = " + QboQuery.Literal(name), name);
        }

        public async Task<QboResultModel<QboItemModel>> GetByIdAsync(string id)
        {
            var response = await _http
                .GetAsync("item/" + id, Entity, "READ", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboItemModel>.FromFailure(response);
            }
            return QboResultModel<QboItemModel>.Ok(
                QboEntityHelper.ReadOne<QboItemModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
