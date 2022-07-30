namespace Listen2MeRefined.Infrastructure.Data.Dapper;

using Microsoft.Data.SqlClient;
using System.Data;

public sealed class DbConnection : IDisposable
{
    private IDbConnection _connection;

    public IDbConnection Connection { get; set; }

    public DbConnection()
    {
        Connection = new SqlConnection(DbInfo.SqliteConnectionString);
        Connection.Open();
    }

    public void Dispose()
    {
        Connection.Dispose();
    }
}