using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public sealed class DataContext : DbContext
{
    public DbSet<AudioModel> Songs { get; set; }
    public DbSet<PlaylistModel> Playlists { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
    public DbSet<MusicFolderModel> MusicFolders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AudioModel>()
            .HasIndex(x => x.Path)
            .IsUnique();

        modelBuilder.Entity<PlaylistModel>()
            .Property(x => x.Name)
            .UseCollation("NOCASE");

        modelBuilder.Entity<PlaylistModel>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<PlaylistModel>()
            .HasMany(x => x.Songs)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "PlaylistSongs",
                right => right
                    .HasOne<AudioModel>()
                    .WithMany()
                    .HasForeignKey("SongId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<PlaylistModel>()
                    .WithMany()
                    .HasForeignKey("PlaylistId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("PlaylistSongs");
                    join.HasKey("PlaylistId", "SongId");
                });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        //optionsBuilder.UseSqlServer(DbInfo.MssqlConnectionString);
        optionsBuilder.UseSqlite(DbInfo.SqliteConnectionString);
    }
}
