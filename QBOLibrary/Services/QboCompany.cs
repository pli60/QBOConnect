using System.Threading.Tasks;
using QBOLibrary.Http;
using QBOLibrary.Models;

namespace QBOLibrary.Services
{
    // Cheapest possible connection health check - always returns exactly one row
    public class QboCompany
    {
        private const string Entity = "CompanyInfo";

        private readonly QboHttp _http;

        public QboCompany(QboHttp http)
        {
            _http = http;
        }

        public Task<QboResultModel<QboCompanyInfoModel>> GetAsync()
        {
            return QboEntityHelper.QueryFirstAsync<QboCompanyInfoModel>(_http, Entity, null);
        }

        // Confirms the realm points at the converted Desktop company
        public async Task<QboResultModel<bool>> IsMigratedCompanyAsync()
        {
            QboResultModel<QboCompanyInfoModel> result = await GetAsync().ConfigureAwait(false);
            if (!result.Success)
            {
                return QboResultModel<bool>.FromFailure(result);
            }
            return QboResultModel<bool>.Ok(result.Data != null && result.Data.IsQbdtMigrated);
        }
    }
}
