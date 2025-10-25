using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportHistoryChunking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "ImportHistories",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedChunks",
                table: "ImportHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ImportHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalChunks",
                table: "ImportHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ImportHistories");

            migrationBuilder.DropColumn(
                name: "ProcessedChunks",
                table: "ImportHistories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ImportHistories");

            migrationBuilder.DropColumn(
                name: "TotalChunks",
                table: "ImportHistories");
        }
    }
}
