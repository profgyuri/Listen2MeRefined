using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsUxOverhaulFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoCheckUpdatesOnStartup",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoScanOnFolderAdd",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<short>(
                name: "CornerTriggerDebounceMs",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: (short)10);

            migrationBuilder.AddColumn<short>(
                name: "CornerTriggerSizePx",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: (short)10);

            migrationBuilder.AddColumn<bool>(
                name: "EnableCornerNowPlayingPopup",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableGlobalMediaKeys",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "StartMuted",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "StartupVolume",
                table: "Settings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.7f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoCheckUpdatesOnStartup",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "AutoScanOnFolderAdd",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CornerTriggerDebounceMs",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CornerTriggerSizePx",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "EnableCornerNowPlayingPopup",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "EnableGlobalMediaKeys",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "StartMuted",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "StartupVolume",
                table: "Settings");
        }
    }
}
