﻿// <auto-generated />
using System;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

            modelBuilder.Entity("AudioModelPlaylistModel", b =>
                {
                    b.Property<int>("PlaylistsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SongsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("PlaylistsId", "SongsId");

                    b.HasIndex("SongsId");

                    b.ToTable("AudioModelPlaylistModel");
                });

            modelBuilder.Entity("Listen2MeRefined.Infrastructure.Data.AppSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FontFamily")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("NewSongWindowPosition")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("ScanOnStartup")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("Listen2MeRefined.Infrastructure.Data.Models.AudioModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Artist")
                        .HasColumnType("TEXT");

                    b.Property<short>("BPM")
                        .HasColumnType("INTEGER");

                    b.Property<short>("Bitrate")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Genre")
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan>("Length")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Songs");
                });

            modelBuilder.Entity("Listen2MeRefined.Infrastructure.Data.Models.MusicFolderModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AppSettingsId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FullPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AppSettingsId");

                    b.ToTable("MusicFolders");
                });

            modelBuilder.Entity("Listen2MeRefined.Infrastructure.Data.Models.PlaylistModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Playlists");
                });

            modelBuilder.Entity("AudioModelPlaylistModel", b =>
                {
                    b.HasOne("Listen2MeRefined.Infrastructure.Data.Models.PlaylistModel", null)
                        .WithMany()
                        .HasForeignKey("PlaylistsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Listen2MeRefined.Infrastructure.Data.Models.AudioModel", null)
                        .WithMany()
                        .HasForeignKey("SongsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Listen2MeRefined.Infrastructure.Data.Models.MusicFolderModel", b =>
                {
                    b.HasOne("Listen2MeRefined.Infrastructure.Data.AppSettings", null)
                        .WithMany("MusicFolders")
                        .HasForeignKey("AppSettingsId");
                });

            modelBuilder.Entity("Listen2MeRefined.Infrastructure.Data.AppSettings", b =>
                {
                    b.Navigation("MusicFolders");
                });
#pragma warning restore 612, 618
        }
    }
}
