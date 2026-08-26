using System.Collections.Generic;
using System.Threading.Tasks;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // TaxCodeRef values for invoice lines
    public class QboTaxCode
    {
        private const string Entity = "TaxCode";

        private readonly QboHttp _http;

        public QboTaxCode(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboTaxCodeModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboTaxCodeModel>(_http, Entity);
        }

        public Task<QboResultModel<List<QboTaxCodeModel>>> QueryActiveAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboTaxCodeModel>(_http, Entity, "Active = true");
        }

        public Task<QboResultModel<QboTaxCodeModel>> QueryByNameAsync(string name)
        {
            return QboEntityHelper.QueryFirstAsync<QboTaxCodeModel>(
                _http, Entity, "Name = " + QboQuery.Literal(name), name);
        }

        public async Task<QboResultModel<QboTaxCodeModel>> GetByIdAsync(string id)
        {
            var response = await _http
                .GetAsync("taxcode/" + id, Entity, "READ", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboTaxCodeModel>.FromFailure(response);
            }
            return QboResultModel<QboTaxCodeModel>.Ok(
                QboEntityHelper.ReadOne<QboTaxCodeModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
