#nullable disable

using System;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Listen2MeRefined.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DataContext))]
    [Migration("20260222170000_AddScanPipelineMetadataAndFolderRecursion")]
    public partial class AddScanPipelineMetadataAndFolderRecursion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeSubdirectories",
                table: "MusicFolders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "LengthBytes",
                table: "Songs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWriteUtc",
                table: "Songs",
                type: "TEXT",
                nullable: false,
                defaultValue: DateTime.UnixEpoch);

            migrationBuilder.Sql(
                """
                DELETE FROM Songs
                WHERE Id IN (
                    SELECT Id
                    FROM (
                        SELECT
                            Id,
                            ROW_NUMBER() OVER (
                                PARTITION BY LOWER(Path)
                                ORDER BY LastWriteUtc DESC, LengthBytes DESC, Id DESC
                            ) AS RowNumber
                        FROM Songs
                        WHERE Path IS NOT NULL
                    )
                    WHERE RowNumber > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Songs_Path",
                table: "Songs",
                column: "Path",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Songs_Path",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "IncludeSubdirectories",
                table: "MusicFolders");

            migrationBuilder.DropColumn(
                name: "LengthBytes",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "LastWriteUtc",
                table: "Songs");
        }
    }
}
