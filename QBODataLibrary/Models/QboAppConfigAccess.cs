using System.Collections.Generic;
using QBODataLibrary.Db;
using QBODataLibrary.Models;

namespace QBODataLibrary
{
    public class QboAppConfigAccess
    {
        private readonly IDataAccess _dataAccess;

        public QboAppConfigAccess(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public QboAppConfigAccess()
        {
            _dataAccess = new SqlDb();
        }

        // Returns null when QBO_APP is missing or has no row for this environment
        public QboAppDbModel Get(string environment)
        {
            string qy = @"select ENVIRONMENT, CLIENT_ID, CLIENT_SECRET, REDIRECT_URI,
                                 USE_QBO_BACKEND, UPDATED_BY, UPDATED_ON
                          from QBO_APP
                          where ENVIRONMENT = @environment";

            List<QboAppDbModel> rows = _dataAccess.GetData<QboAppDbModel, dynamic>(
                qy, new { environment = environment });

            if (rows == null || rows.Count == 0)
            {
                return null;
            }
            return rows[0];
        }
    }
}