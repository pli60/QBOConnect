using System.Collections.Generic;
using System.Threading.Tasks;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // ITermsQuery
    public class QboTerm
    {
        private const string Entity = "Term";

        private readonly QboHttp _http;

        public QboTerm(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<List<QboTermModel>>> QueryAllAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboTermModel>(_http, Entity);
        }

        public Task<QboResultModel<List<QboTermModel>>> QueryActiveAsync()
        {
            return QboEntityHelper.QueryPagedAsync<QboTermModel>(_http, Entity, "Active = true");
        }

        public Task<QboResultModel<QboTermModel>> QueryByNameAsync(string name)
        {
            return QboEntityHelper.QueryFirstAsync<QboTermModel>(
                _http, Entity, "Name = " + QboQuery.Literal(name), name);
        }

        public async Task<QboResultModel<QboTermModel>> GetByIdAsync(string id)
        {
            var response = await _http
                .GetAsync("term/" + id, Entity, "READ", id)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<QboTermModel>.FromFailure(response);
            }
            return QboResultModel<QboTermModel>.Ok(
                QboEntityHelper.ReadOne<QboTermModel>(response.Data, Entity),
                response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
