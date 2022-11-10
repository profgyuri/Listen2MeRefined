namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;

public sealed class DataContext : DbContext
{
#pragma warning disable CS8618
    public DbSet<AudioModel> Songs { get; set; }
    public DbSet<PlaylistModel> Playlists { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
#pragma warning restore CS8618

    public DataContext()
    {
        Database.EnsureCreated();
    }

    #region Overrides of DbContext
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettings>().HasNoKey();
    }
    #endregion

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        //optionsBuilder.UseSqlServer(DbInfo.MssqlConnectionString);
        optionsBuilder.UseSqlite(DbInfo.SqliteConnectionString);
    }
}