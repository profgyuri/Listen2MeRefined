namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;

public class DataContext : DbContext
{
    private const string mssqlConnectionString = "Data Source=.;Initial Catalog=listentome;Integrated Security=True";
    private const string sqliteConnectionString = "Data Source=listentome.db;";

#pragma warning disable CS8618
    public DbSet<AudioModel> Songs { get; set; }
    public DbSet<PlaylistModel> Playlists { get; set; }
#pragma warning restore CS8618

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        //optionsBuilder.UseSqlServer(mssqlConnectionString);
        optionsBuilder.UseSqlite(sqliteConnectionString);
    }
}