namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

public sealed class DataContext : DbContext
{
    public DbSet<AudioModel> Songs { get; set; }
    public DbSet<PlaylistModel> Playlists { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
    public DbSet<MusicFolderModel> MusicFolders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        //optionsBuilder.UseSqlServer(DbInfo.MssqlConnectionString);
        optionsBuilder.UseSqlite(DbInfo.SqliteConnectionString);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure many-to-many relationship
        modelBuilder.Entity<AudioModel>()
            .HasMany(s => s.Playlists)
            .WithMany(p => p.Songs); // Junction table
            
        base.OnModelCreating(modelBuilder);
    }
}