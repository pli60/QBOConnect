using System.Collections.Generic;
using System.Threading.Tasks;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // read only via the API
    public class QboCustomerType
    {
        private const string Entity = "CustomerType";

        private readonly QboHttp _http;

        public QboCustomerType(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboCustomerTypeModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboCustomerTypeModel>(_http, Entity);
        }

        public Task<QboResultModel<List<QboCustomerTypeModel>>> QueryActiveAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboCustomerTypeModel>(_http, Entity, "Active = true");
        }

        public Task<QboResultModel<QboCustomerTypeModel>> QueryByNameAsync(string name)
        {
            return QboEntityHelper.QueryFirstAsync<QboCustomerTypeModel>(
                _http, Entity, "Name = " + QboQuery.Literal(name), name);
        }

        public async Task<QboResultModel<QboCustomerTypeModel>> GetByIdAsync(string id)
        {
            var response = await _http
                .GetAsync("customertype/" + id, Entity, "READ", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboCustomerTypeModel>.FromFailure(response);
            }
            return QboResultModel<QboCustomerTypeModel>.Ok(
                QboEntityHelper.ReadOne<QboCustomerTypeModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
