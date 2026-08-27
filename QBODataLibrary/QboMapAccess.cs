using System.Collections.Generic;
using System.Data;
using System.Linq;
using QBODataLibrary.Db;
using QBODataLibrary.Models;
using QBOLibrary;
using QBOLibrary.Services;

namespace QBODataLibrary
{
    public class QboMapAccess : IQboMapStore
    {
        private readonly IDataAccess _dataAccess;

        public QboMapAccess(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public QboMapAccess()
        {
            _dataAccess = new SqlDb();
        }

        public List<QboMapDbModel> GetByEntity(string entity)
        {
            string qy = @"select ID, ENVIRONMENT, ENTITY, LIMS_KEY, LIMS_NAME,
                                 QBO_ID, QBO_NAME, MATCH_STATUS, UPDATED_BY, UPDATED_ON
                          from QBO_MAP
                          where ENVIRONMENT = @environment
                            and ENTITY      = @entity";

            return _dataAccess.GetData<QboMapDbModel, dynamic>(qy,
                new { environment = QboConfig.Environment, entity = entity })
                ?? new List<QboMapDbModel>();
        }

        public List<QboMapDbModel> GetUnmatched(string entity)
        {
            string qy = @"select ID, ENVIRONMENT, ENTITY, LIMS_KEY, LIMS_NAME,
                                 QBO_ID, QBO_NAME, MATCH_STATUS, UPDATED_BY, UPDATED_ON
                          from QBO_MAP
                          where ENVIRONMENT  = @environment
                            and ENTITY       = @entity
                            and MATCH_STATUS in ('UNMATCHED', 'CONFLICT')";

            return _dataAccess.GetData<QboMapDbModel, dynamic>(qy,
                new { environment = QboConfig.Environment, entity = entity })
                ?? new List<QboMapDbModel>();
        }

        // IQboMapStore
        public string GetQboId(string entity, string limsKey)
        {
            string qy = @"select QBO_ID
                          from QBO_MAP
                          where ENVIRONMENT = @environment
                            and ENTITY      = @entity
                            and LIMS_KEY    = @limskey";

            var rows = _dataAccess.GetData<string, dynamic>(qy,
                new { environment = QboConfig.Environment, entity = entity, limskey = limsKey });

            return rows?.FirstOrDefault();
        }

        // IQboMapStore
        public void Upsert(string entity, string limsKey, string limsName,
                           string qboId, string qboName, string matchStatus, string updatedBy)
        {
            Upsert(new QboMapDbModel
            {
                Entity = entity,
                Lims_key = limsKey,
                Lims_name = limsName,
                Qbo_id = qboId,
                Qbo_name = qboName,
                Match_status = matchStatus,
                Updated_by = updatedBy
            });
        }

        public void Upsert(QboMapDbModel map)
        {
            string qy = @"update QBO_MAP
                          set LIMS_NAME    = @limsname,
                              QBO_ID       = @qboid,
                              QBO_NAME     = @qboname,
                              MATCH_STATUS = @matchstatus,
                              UPDATED_BY   = @updatedby,
                              UPDATED_ON   = GETDATE()
                          where ENVIRONMENT = @environment
                            and ENTITY      = @entity
                            and LIMS_KEY    = @limskey;

                          if @@ROWCOUNT = 0
                              insert into QBO_MAP
                                  (ENVIRONMENT, ENTITY, LIMS_KEY, LIMS_NAME,
                                   QBO_ID, QBO_NAME, MATCH_STATUS, UPDATED_BY, UPDATED_ON)
                              values
                                  (@environment, @entity, @limskey, @limsname,
                                   @qboid, @qboname, @matchstatus, @updatedby, GETDATE());
                          select 1;";

            _dataAccess.Save<dynamic>(qy,
                new
                {
                    environment = QboConfig.Environment,
                    entity      = map.Entity,
                    limskey     = map.Lims_key,
                    limsname    = map.Lims_name,
                    qboid       = map.Qbo_id,
                    qboname     = map.Qbo_name,
                    matchstatus = map.Match_status,
                    updatedby   = map.Updated_by
                },
                CommandType.Text);
        }

        public void UpsertMany(List<QboMapDbModel> maps)
        {
            foreach (QboMapDbModel map in maps)
            {
                Upsert(map);
            }
        }
    }
}
