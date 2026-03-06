#nullable disable

using System;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Listen2MeRefined.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(DataContext))]
[Migration("20260301130000_AddPlaylistManyToManyAndTransferModeSetting")]
public partial class AddPlaylistManyToManyAndTransferModeSetting : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SearchResultsTransferMode",
            table: "Settings",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        if (string.Equals(ActiveProvider, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Songs_PlaylistModelId\";");
            migrationBuilder.Sql("DELETE FROM Playlists;");
            migrationBuilder.Sql(
                """
                CREATE TABLE "Songs_tmp" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Songs" PRIMARY KEY AUTOINCREMENT,
                    "Artist" TEXT NULL,
                    "Title" TEXT NULL,
                    "Genre" TEXT NULL,
                    "BPM" INTEGER NOT NULL,
                    "Bitrate" INTEGER NOT NULL,
                    "Length" TEXT NOT NULL,
                    "Path" TEXT NULL,
                    "LengthBytes" INTEGER NOT NULL,
                    "LastWriteUtc" TEXT NOT NULL
                );

                INSERT INTO "Songs_tmp" ("Id", "Artist", "Title", "Genre", "BPM", "Bitrate", "Length", "Path", "LengthBytes", "LastWriteUtc")
                SELECT "Id", "Artist", "Title", "Genre", "BPM", "Bitrate", "Length", "Path", "LengthBytes", "LastWriteUtc"
                FROM "Songs";

                DROP TABLE "Songs";
                ALTER TABLE "Songs_tmp" RENAME TO "Songs";
                CREATE UNIQUE INDEX "IX_Songs_Path" ON "Songs" ("Path");
                """);
        }
        else
        {
            migrationBuilder.CreateTable(
                name: "PlaylistSongs",
                columns: table => new
                {
                    PlaylistId = table.Column<int>(type: "INTEGER", nullable: false),
                    SongId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistSongs", x => new { x.PlaylistId, x.SongId });
                    table.ForeignKey(
                        name: "FK_PlaylistSongs_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaylistSongs_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistSongs_SongId",
                table: "PlaylistSongs",
                column: "SongId");

            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Playlists_PlaylistModelId",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_PlaylistModelId",
                table: "Songs");

            migrationBuilder.Sql("DELETE FROM Playlists;");
            migrationBuilder.Sql("ALTER TABLE [Songs] DROP COLUMN [PlaylistModelId];");
        }

        if (string.Equals(ActiveProvider, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            migrationBuilder.CreateTable(
                name: "PlaylistSongs",
                columns: table => new
                {
                    PlaylistId = table.Column<int>(type: "INTEGER", nullable: false),
                    SongId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistSongs", x => new { x.PlaylistId, x.SongId });
                    table.ForeignKey(
                        name: "FK_PlaylistSongs_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaylistSongs_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistSongs_SongId",
                table: "PlaylistSongs",
                column: "SongId");
        }

        migrationBuilder.Sql("CREATE UNIQUE INDEX IX_Playlists_Name ON Playlists (Name COLLATE NOCASE);");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Playlists_Name;");

        migrationBuilder.DropTable(
            name: "PlaylistSongs");

        migrationBuilder.DropColumn(
            name: "SearchResultsTransferMode",
            table: "Settings");

        migrationBuilder.AddColumn<int>(
            name: "PlaylistModelId",
            table: "Songs",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Songs_PlaylistModelId",
            table: "Songs",
            column: "PlaylistModelId");

        if (!string.Equals(ActiveProvider, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Songs_Playlists_PlaylistModelId",
                table: "Songs",
                column: "PlaylistModelId",
                principalTable: "Playlists",
                principalColumn: "Id");
        }
    }
}
