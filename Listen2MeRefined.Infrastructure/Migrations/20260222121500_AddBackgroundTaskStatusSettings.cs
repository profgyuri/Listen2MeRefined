using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listen2MeRefined.Infrastructure.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20260222121500_AddBackgroundTaskStatusSettings")]
public partial class AddBackgroundTaskStatusSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ScanMilestoneBasis",
            table: "Settings",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<short>(
            name: "ScanMilestoneInterval",
            table: "Settings",
            type: "INTEGER",
            nullable: false,
            defaultValue: (short)25);

        migrationBuilder.AddColumn<bool>(
            name: "ShowScanMilestoneCount",
            table: "Settings",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ShowTaskPercentage",
            table: "Settings",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<short>(
            name: "TaskPercentageReportInterval",
            table: "Settings",
            type: "INTEGER",
            nullable: false,
            defaultValue: (short)1);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ScanMilestoneBasis",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "ScanMilestoneInterval",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "ShowScanMilestoneCount",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "ShowTaskPercentage",
            table: "Settings");

        migrationBuilder.DropColumn(
            name: "TaskPercentageReportInterval",
            table: "Settings");
    }
}
