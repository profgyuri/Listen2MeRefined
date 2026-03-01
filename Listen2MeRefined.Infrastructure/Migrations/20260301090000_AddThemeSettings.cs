using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations;

public partial class AddThemeSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AccentColor",
            table: "Settings",
            type: "TEXT",
            nullable: false,
            defaultValue: "Orange");

        migrationBuilder.AddColumn<string>(
            name: "ThemeMode",
            table: "Settings",
            type: "TEXT",
            nullable: false,
            defaultValue: "Dark");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AccentColor",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "ThemeMode",
            table: "Settings");
    }
}
