namespace RAHM.BD.Services;

using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
public interface IDb
{
    string GetConnectionString();
    Task<T?> QuerySingleAsync<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters);
    Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters);
    Task<int> ExecuteAsync(string sql, params SqlParameter[] parameters);
}
