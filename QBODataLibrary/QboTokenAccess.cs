using System;
using System.Collections.Generic;
using System.Data;
using QBODataLibrary.Db;
using QBODataLibrary.Models;
using QBOLibrary;
using QBOLibrary.Auth;

namespace QBODataLibrary
{
    public class QboTokenAccess : IQboTokenStore
    {
        private readonly IDataAccess _dataAccess;

        public QboTokenAccess(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public QboTokenAccess()
        {
            _dataAccess = new SqlDb();
        }

        public QboTokenModel Get()
        {
            string qy = @"select ID, ENVIRONMENT, REALM_ID, ACCESS_TOKEN, ACCESS_EXPIRES,
                                 REFRESH_TOKEN, REFRESH_EXPIRES, REFRESH_LOCKED,
                                 LOCKED_BY, LOCKED_AT, UPDATED_BY, UPDATED_ON
                          from QBO_TOKEN
                          where ENVIRONMENT = @environment";

            List<QboTokenDbModel> rows = _dataAccess.GetData<QboTokenDbModel, dynamic>(
                qy, new { environment = QboConfig.Environment });

            if (rows == null || rows.Count == 0)
            {
                return null;
            }
            return MapToModel(rows[0]);
        }

        // Atomic single-statement lock; stale locks are reclaimed after staleLockSeconds
        public bool TryLockForRefresh(string owner, int staleLockSeconds)
        {
            string qy = @"update QBO_TOKEN
                          set REFRESH_LOCKED = 1,
                              LOCKED_BY      = @owner,
                              LOCKED_AT      = GETDATE()
                          where ENVIRONMENT = @environment
                            and (REFRESH_LOCKED = 0
                                 or LOCKED_AT is null
                                 or LOCKED_AT < DATEADD(SECOND, -@stale, GETDATE()));
                          select @@ROWCOUNT;";

            int rows = _dataAccess.Save<dynamic>(qy,
                new
                {
                    environment = QboConfig.Environment,
                    owner = owner,
                    stale = staleLockSeconds
                },
                CommandType.Text);

            return rows == 1;
        }

        // LOCKED_BY guard stops a stale holder overwriting a newer token
        public void SaveRefreshed(QboTokenModel token, string owner)
        {
            string qy = @"update QBO_TOKEN
                          set ACCESS_TOKEN    = @accesstoken,
                              ACCESS_EXPIRES  = @accessexpires,
                              REFRESH_TOKEN   = @refreshtoken,
                              REFRESH_EXPIRES = @refreshexpires,
                              REFRESH_LOCKED  = 0,
                              LOCKED_BY       = null,
                              LOCKED_AT       = null,
                              UPDATED_BY      = @owner,
                              UPDATED_ON      = GETDATE()
                          where ENVIRONMENT = @environment
                            and LOCKED_BY   = @owner;
                          select @@ROWCOUNT;";

            _dataAccess.Save<dynamic>(qy,
                new
                {
                    environment = QboConfig.Environment,
                    accesstoken = token.AccessToken,
                    accessexpires = token.AccessExpires,
                    refreshtoken = token.RefreshToken,
                    refreshexpires = token.RefreshExpires,
                    owner = owner
                },
                CommandType.Text);
        }

        // Initial OAuth consent - no lock is held, row may not exist yet
        public void SaveAuthorized(QboTokenModel token, string owner)
        {
            string qy = @"update QBO_TOKEN
                          set REALM_ID        = @realmid,
                              ACCESS_TOKEN    = @accesstoken,
                              ACCESS_EXPIRES  = @accessexpires,
                              REFRESH_TOKEN   = @refreshtoken,
                              REFRESH_EXPIRES = @refreshexpires,
                              REFRESH_LOCKED  = 0,
                              LOCKED_BY       = null,
                              LOCKED_AT       = null,
                              UPDATED_BY      = @owner,
                              UPDATED_ON      = GETDATE()
                          where ENVIRONMENT = @environment;

                          if @@ROWCOUNT = 0
                              insert into QBO_TOKEN
                                  (ENVIRONMENT, REALM_ID, ACCESS_TOKEN, ACCESS_EXPIRES,
                                   REFRESH_TOKEN, REFRESH_EXPIRES, REFRESH_LOCKED,
                                   UPDATED_BY, UPDATED_ON)
                              values
                                  (@environment, @realmid, @accesstoken, @accessexpires,
                                   @refreshtoken, @refreshexpires, 0,
                                   @owner, GETDATE());
                          select 1;";

            _dataAccess.Save<dynamic>(qy,
                new
                {
                    environment = QboConfig.Environment,
                    realmid = token.RealmId,
                    accesstoken = token.AccessToken,
                    accessexpires = token.AccessExpires,
                    refreshtoken = token.RefreshToken,
                    refreshexpires = token.RefreshExpires,
                    owner = owner
                },
                CommandType.Text);
        }

        public void ReleaseLock(string owner)
        {
            string qy = @"update QBO_TOKEN
                          set REFRESH_LOCKED = 0,
                              LOCKED_BY      = null,
                              LOCKED_AT      = null
                          where ENVIRONMENT = @environment
                            and LOCKED_BY   = @owner;
                          select @@ROWCOUNT;";

            _dataAccess.Save<dynamic>(qy,
                new { environment = QboConfig.Environment, owner = owner },
                CommandType.Text);
        }

        private static QboTokenModel MapToModel(QboTokenDbModel dbModel)
        {
            return new QboTokenModel
            {
                Environment = dbModel.Environment,
                RealmId = dbModel.Realm_id,
                AccessToken = dbModel.Access_token,
                AccessExpires = dbModel.Access_expires,
                RefreshToken = dbModel.Refresh_token,
                RefreshExpires = dbModel.Refresh_expires,
                RefreshLocked = dbModel.Refresh_locked,
                LockedBy = dbModel.Locked_by,
                LockedAt = dbModel.Locked_at
            };
        }
    }
}