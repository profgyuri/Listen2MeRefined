using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FontFamily = table.Column<string>(type: "TEXT", nullable: false),
                    ScanOnStartup = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Songs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Artist = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Genre = table.Column<string>(type: "TEXT", nullable: true),
                    BPM = table.Column<short>(type: "INTEGER", nullable: false),
                    Bitrate = table.Column<short>(type: "INTEGER", nullable: false),
                    Length = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    PlaylistModelId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Songs_Playlists_PlaylistModelId",
                        column: x => x.PlaylistModelId,
                        principalTable: "Playlists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MusicFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullPath = table.Column<string>(type: "TEXT", nullable: false),
                    AppSettingsId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicFolders_Settings_AppSettingsId",
                        column: x => x.AppSettingsId,
                        principalTable: "Settings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicFolders_AppSettingsId",
                table: "MusicFolders",
                column: "AppSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_PlaylistModelId",
                table: "Songs",
                column: "PlaylistModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicFolders");

            migrationBuilder.DropTable(
                name: "Songs");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Playlists");
        }
    }
}
