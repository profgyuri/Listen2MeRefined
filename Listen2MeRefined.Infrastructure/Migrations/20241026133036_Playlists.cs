using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Playlists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Playlists_PlaylistModelId",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Songs_PlaylistModelId",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "PlaylistModelId",
                table: "Songs");

            migrationBuilder.CreateTable(
                name: "AudioModelPlaylistModel",
                columns: table => new
                {
                    PlaylistsId = table.Column<int>(type: "INTEGER", nullable: false),
                    SongsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioModelPlaylistModel", x => new { x.PlaylistsId, x.SongsId });
                    table.ForeignKey(
                        name: "FK_AudioModelPlaylistModel_Playlists_PlaylistsId",
                        column: x => x.PlaylistsId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AudioModelPlaylistModel_Songs_SongsId",
                        column: x => x.SongsId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioModelPlaylistModel_SongsId",
                table: "AudioModelPlaylistModel",
                column: "SongsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioModelPlaylistModel");

            migrationBuilder.AddColumn<int>(
                name: "PlaylistModelId",
                table: "Songs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Songs_PlaylistModelId",
                table: "Songs",
                column: "PlaylistModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_Playlists_PlaylistModelId",
                table: "Songs",
                column: "PlaylistModelId",
                principalTable: "Playlists",
                principalColumn: "Id");
        }
    }
}
