using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportHistoryBlobSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobPath",
                table: "ImportHistories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentPosition",
                table: "ImportHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "ImportHistories",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationSent",
                table: "ImportHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedAt",
                table: "ImportHistories",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlobPath",
                table: "ImportHistories");

            migrationBuilder.DropColumn(
                name: "CurrentPosition",
                table: "ImportHistories");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "ImportHistories");

            migrationBuilder.DropColumn(
                name: "NotificationSent",
                table: "ImportHistories");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "ImportHistories");
        }
    }
}
