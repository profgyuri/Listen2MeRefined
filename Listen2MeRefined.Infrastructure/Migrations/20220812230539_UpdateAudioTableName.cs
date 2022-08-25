using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations
{
    public partial class UpdateAudioTableName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_songs_Playlists_PlaylistModelId",
                table: "songs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_songs",
                table: "songs");

            migrationBuilder.RenameTable(
                name: "songs",
                newName: "Songs");

            migrationBuilder.RenameIndex(
                name: "IX_songs_PlaylistModelId",
                table: "Songs",
                newName: "IX_Songs_PlaylistModelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Songs",
                table: "Songs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_Playlists_PlaylistModelId",
                table: "Songs",
                column: "PlaylistModelId",
                principalTable: "Playlists",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Playlists_PlaylistModelId",
                table: "Songs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Songs",
                table: "Songs");

            migrationBuilder.RenameTable(
                name: "Songs",
                newName: "songs");

            migrationBuilder.RenameIndex(
                name: "IX_Songs_PlaylistModelId",
                table: "songs",
                newName: "IX_songs_PlaylistModelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_songs",
                table: "songs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_songs_Playlists_PlaylistModelId",
                table: "songs",
                column: "PlaylistModelId",
                principalTable: "Playlists",
                principalColumn: "Id");
        }
    }
}
