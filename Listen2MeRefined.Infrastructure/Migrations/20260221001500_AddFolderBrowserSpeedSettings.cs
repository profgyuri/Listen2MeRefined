using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderBrowserSpeedSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FolderBrowserStartAtLastLocation",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LastBrowsedFolder",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PinnedFoldersJson",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FolderBrowserStartAtLastLocation",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "LastBrowsedFolder",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PinnedFoldersJson",
                table: "Settings");
        }
    }
}
