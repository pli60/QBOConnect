using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace QBODataLibrary.Db
{
    public interface IDataAccess
    {
        string LastError { get; }

        Task<List<T>> LoadData<T, U>(string sp, U parameters);
        List<T> GetData<T, U>(string sp, U parameters);
        Task<int> SaveData<T>(string sp, T parameters, CommandType commandType);
        int Save<T>(string sp, T parameters, CommandType commandType);
        bool DeleteList<T>(string sp, T parameters, CommandType commandType);
    }
}