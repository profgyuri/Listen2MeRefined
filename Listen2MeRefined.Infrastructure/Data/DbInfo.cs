namespace Listen2MeRefined.Infrastructure.Data;

public static class DbInfo
{
    private const string mssqlConnectionString = "Data Source=.;Initial Catalog=listentome;Integrated Security=True";
    private const string sqliteConnectionString = "Data Source=listentome.db;";

    public static string MssqlConnectionString => mssqlConnectionString;
    public static string SqliteConnectionString => sqliteConnectionString;
}
