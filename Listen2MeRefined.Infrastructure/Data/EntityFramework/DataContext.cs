﻿using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.EntityFramework;

public sealed class DataContext : DbContext
{
    public DbSet<AudioModel> Songs { get; set; }
    public DbSet<PlaylistModel> Playlists { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
    public DbSet<MusicFolderModel> MusicFolders { get; set; }

    public DataContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        //optionsBuilder.UseSqlServer(DbInfo.MssqlConnectionString);
        optionsBuilder.UseSqlite(DbInfo.SqliteConnectionString);
    }
}