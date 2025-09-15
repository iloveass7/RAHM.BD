using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RAHM.BD.Services;
using Microsoft.Data.SqlClient;
public class Db : IDb
{
    private readonly string _connectionString;

    public Db(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection GetConnection() => new SqlConnection(_connectionString);

<<<<<<< HEAD
=======
    public string GetConnectionString() => _connectionString;

>>>>>>> iloveass-clean
    public async Task<T?> QuerySingleAsync<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        if (parameters?.Length > 0) cmd.Parameters.AddRange(parameters);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return map(reader);
        }
        return default;
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
    {
        var results = new List<T>();
        await using var conn = GetConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        if (parameters?.Length > 0) cmd.Parameters.AddRange(parameters);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }
        return results;
    }

    public async Task<int> ExecuteAsync(string sql, params SqlParameter[] parameters)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        if (parameters?.Length > 0) cmd.Parameters.AddRange(parameters);

        return await cmd.ExecuteNonQueryAsync();
    }
}
