using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistPinAndOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Playlists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Playlists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Playlists");
        }
    }
}
