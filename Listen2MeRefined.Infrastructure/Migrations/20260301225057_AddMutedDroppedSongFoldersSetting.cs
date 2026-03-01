using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMutedDroppedSongFoldersSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MutedDroppedSongFoldersJson",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MutedDroppedSongFoldersJson",
                table: "Settings");
        }
    }
}
