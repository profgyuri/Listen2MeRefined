#nullable disable

using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Listen2MeRefined.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DataContext))]
    [Migration("20260301000000_AddPlaylistViewModeSetting")]
    public partial class AddPlaylistViewModeSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseCompactPlaylistView",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseCompactPlaylistView",
                table: "Settings");
        }
    }
}
