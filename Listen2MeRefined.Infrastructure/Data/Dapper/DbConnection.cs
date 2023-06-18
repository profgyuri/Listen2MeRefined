using Microsoft.Data.SqlClient;

namespace Listen2MeRefined.Infrastructure.Data.Dapper;

public sealed class DbConnection : IDisposable
{
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