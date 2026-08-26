using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QBOLibrary.Models;

namespace QBOLibrary.Http
{
    // Shared read/paging plumbing used by every service
    public static class QboEntityHelper
    {
        // QueryResponse returns an array for multiple rows and a bare object for one
        public static List<T> ReadList<T>(JObject json, string entity)
        {
            JToken node = json?["QueryResponse"]?[entity];
            if (node == null)
            {
                return new List<T>();
            }
            if (node.Type == JTokenType.Array)
            {
                return node.ToObject<List<T>>();
            }
            return new List<T> { node.ToObject<T>() };
        }

        public static T ReadOne<T>(JObject json, string entity) where T : class
        {
            JToken node = json?[entity];
            return node?.ToObject<T>();
        }

        public static async Task<QboResultModel<List<T>>> QueryPagedAsync<T>(
            QboHttp http, string entity, string where = null, string orderBy = null, string limsKey = null)
        {
            List<T> all = new List<T>();
            int startPosition = 1;

            while (true)
            {
                string query = QboQuery.Build(entity, where, orderBy, startPosition, QboQuery.MaxPageSize);
                QboResultModel<JObject> response = await http
                    .GetAsync(QboQuery.Path(query), entity, "QUERY", limsKey)
                    .ConfigureAwait(false);

                if (!response.Success)
                {
                    return QboResultModel<List<T>>.FromFailure(response);
                }

                List<T> page = ReadList<T>(response.Data, entity);
                all.AddRange(page);

                // A short page means the end of the result set
                if (page.Count < QboQuery.MaxPageSize)
                {
                    break;
                }
                if (all.Count >= QboQuery.MaxTotalRecords)
                {
                    break;
                }
                startPosition += page.Count;
            }

            return QboResultModel<List<T>>.Ok(all);
        }

        public static async Task<QboResultModel<T>> QueryFirstAsync<T>(
            QboHttp http, string entity, string where, string limsKey = null) where T : class
        {
            string query = QboQuery.Build(entity, where, null, 1, 1);
            QboResultModel<JObject> response = await http
                .GetAsync(QboQuery.Path(query), entity, "QUERY", limsKey)
                .ConfigureAwait(false);

            if (!response.Success)
            {
                return QboResultModel<T>.FromFailure(response);
            }

            List<T> rows = ReadList<T>(response.Data, entity);
            return QboResultModel<T>.Ok(rows.Count > 0 ? rows[0] : null,
                                        response.HttpStatus, response.IntuitTid, response.RawResponse);
        }
    }
}
