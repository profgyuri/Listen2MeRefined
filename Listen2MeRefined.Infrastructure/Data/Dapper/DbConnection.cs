namespace Listen2MeRefined.Infrastructure.Data.Dapper;
using Microsoft.Data.SqlClient;

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