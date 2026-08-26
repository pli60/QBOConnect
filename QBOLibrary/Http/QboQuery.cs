using System;
using System.Text;

namespace QBOLibrary.Http
{
    // QBO query language: AND only (no OR), operators = < > <= >= IN LIKE
    public static class QboQuery
    {
        public const int MaxPageSize = 1000;

        // Safety valve so a paging loop can never run away
        public const int MaxTotalRecords = 50000;

        // Single quotes inside literals are escaped with a backslash
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        public static string Literal(string value)
        {
            return "'" + Escape(value) + "'";
        }

        public static string Date(DateTime value)
        {
            return "'" + value.ToString("yyyy-MM-dd") + "'";
        }

        public static string Build(string entity, string where = null, string orderBy = null,
                                   int startPosition = 1, int maxResults = MaxPageSize)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from ").Append(entity);

            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append(" where ").Append(where);
            }
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                sb.Append(" orderby ").Append(orderBy);
            }

            sb.Append(" startposition ").Append(startPosition);
            sb.Append(" maxresults ").Append(maxResults);
            return sb.ToString();
        }

        public static string BuildCount(string entity, string where = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select count(*) from ").Append(entity);
            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append(" where ").Append(where);
            }
            return sb.ToString();
        }

        public static string Path(string query)
        {
            return "query?query=" + Uri.EscapeDataString(query);
        }
    }
}
