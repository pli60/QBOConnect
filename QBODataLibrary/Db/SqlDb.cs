using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace QBODataLibrary.Db
{
    public class SqlDb : IDataAccess
    {
        private readonly string _connectionString;

        public string LastError { get; private set; }

        public SqlDb()
        {
            this._connectionString = Properties.Settings.Default.SQLConnectionString;
        }

        public SqlDb(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public async Task<List<T>> LoadData<T, U>(string sp, U parameters)
        {
            using (IDbConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    var rows = await conn.QueryAsync<T>(sp, parameters, commandType: CommandType.Text);
                    LastError = null;
                    return rows.ToList();
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    return null;
                }
            }
        }

        public List<T> GetData<T, U>(string sp, U parameters)
        {
            using (IDbConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    var rows = conn.Query<T>(sp, parameters, commandType: CommandType.Text);
                    LastError = null;
                    return rows.ToList();
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    return null;
                }
            }
        }

        public async Task<int> SaveData<T>(string sp, T parameters, CommandType commandType)
        {
            using (IDbConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    int result = await conn.ExecuteScalarAsync<int>(sp, parameters, commandType: commandType);
                    LastError = null;
                    return result;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    return -1;
                }
            }
        }

        public int Save<T>(string sp, T parameters, CommandType commandType)
        {
            using (IDbConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    int result = conn.ExecuteScalar<int>(sp, parameters, commandType: commandType);
                    LastError = null;
                    return result;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    return -1;
                }
            }
        }

        public bool DeleteList<T>(string sp, T parameters, CommandType commandType)
        {
            using (IDbConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    conn.Execute(sp, parameters, commandType: commandType);
                    LastError = null;
                    return true;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    return false;
                }
            }
        }
    }
}