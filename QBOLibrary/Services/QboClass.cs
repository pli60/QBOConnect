using System.Collections.Generic;
using System.Threading.Tasks;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // Maps to INVOICE_ITEM.QB_CUSTOMER_CLASS and QB_CUSTOMER_CLASS.Full_name
    public class QboClass
    {
        private const string Entity = "Class";

        private readonly QboHttp _http;

        public QboClass(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboClassModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboClassModel>(_http, Entity);
        }

        public Task<QboResultModel<List<QboClassModel>>> QueryActiveAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboClassModel>(_http, Entity, "Active = true");
        }

        public Task<QboResultModel<QboClassModel>> QueryByNameAsync(string name)
        {
            return QboEntityHelper.QueryFirstAsync<QboClassModel>(
                _http, Entity, "Name = " + QboQuery.Literal(name), name);
        }

        public async Task<QboResultModel<QboClassModel>> GetByIdAsync(string id)
        {
            var response = await _http.GetAsync("class/" + id, Entity, "READ", id).ConfigureAwait(false);
            if (!response.Success)
            {
                return QboResultModel<QboClassModel>.FromFailure(response);
            }
            return QboResultModel<QboClassModel>.Ok(
                QboEntityHelper.ReadOne<QboClassModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
