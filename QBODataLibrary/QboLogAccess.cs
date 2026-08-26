using System;
using System.Collections.Generic;
using System.Data;
using QBODataLibrary.Db;
using QBODataLibrary.Models;
using QBOLibrary;
using QBOLibrary.Http;

namespace QBODataLibrary
{
    public class QboLogAccess : IQboLogStore
    {
        private const int MaxErrorMsgLength = 1000;

        private readonly IDataAccess _dataAccess;

        public QboLogAccess(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public QboLogAccess()
        {
            _dataAccess = new SqlDb();
        }

        // Logging must never break the caller
        public void Write(QboLogModel entry)
        {
            if (entry == null)
            {
                return;
            }

            try
            {
                string qy = @"insert into QBO_API_LOG
                                  (ENVIRONMENT, ENTITY, OPERATION, LIMS_KEY, QBO_ID,
                                   HTTP_STATUS, INTUIT_TID, ERROR_CODE, ERROR_MSG,
                                   REQUEST_JSON, RESPONSE_JSON, CREATED_BY, CREATED_ON)
                              values
                                  (@environment, @entity, @operation, @limskey, @qboid,
                                   @httpstatus, @intuittid, @errorcode, @errormsg,
                                   @requestjson, @responsejson, @createdby, GETDATE());
                              select 1;";

                _dataAccess.Save<dynamic>(qy,
                    new
                    {
                        environment = entry.Environment ?? QboConfig.Environment,
                        entity = entry.Entity,
                        operation = entry.Operation,
                        limskey = entry.LimsKey,
                        qboid = entry.QboId,
                        httpstatus = entry.HttpStatus,
                        intuittid = entry.IntuitTid,
                        errorcode = entry.ErrorCode,
                        errormsg = Truncate(entry.ErrorMsg, MaxErrorMsgLength),
                        requestjson = entry.RequestJson,
                        responsejson = entry.ResponseJson,
                        createdby = entry.CreatedBy
                    },
                    CommandType.Text);
            }
            catch (Exception)
            {
                // Swallow - an audit failure must not abort a QuickBooks operation
            }
        }

        public List<QboLogDbModel> GetRecent(int top)
        {
            string qy = @"select top (@top)
                                 ID, ENVIRONMENT, ENTITY, OPERATION, LIMS_KEY, QBO_ID,
                                 HTTP_STATUS, INTUIT_TID, ERROR_CODE, ERROR_MSG,
                                 REQUEST_JSON, RESPONSE_JSON, CREATED_BY, CREATED_ON
                          from QBO_API_LOG
                          where ENVIRONMENT = @environment
                          order by ID desc";

            return _dataAccess.GetData<QboLogDbModel, dynamic>(qy,
                new { top = top, environment = QboConfig.Environment })
                ?? new List<QboLogDbModel>();
        }

        public List<QboLogDbModel> GetErrors(int top)
        {
            string qy = @"select top (@top)
                                 ID, ENVIRONMENT, ENTITY, OPERATION, LIMS_KEY, QBO_ID,
                                 HTTP_STATUS, INTUIT_TID, ERROR_CODE, ERROR_MSG,
                                 REQUEST_JSON, RESPONSE_JSON, CREATED_BY, CREATED_ON
                          from QBO_API_LOG
                          where ENVIRONMENT = @environment
                            and (ERROR_CODE is not null or HTTP_STATUS >= 400)
                          order by ID desc";

            return _dataAccess.GetData<QboLogDbModel, dynamic>(qy,
                new { top = top, environment = QboConfig.Environment })
                ?? new List<QboLogDbModel>();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }
            return value.Substring(0, maxLength);
        }
    }
}