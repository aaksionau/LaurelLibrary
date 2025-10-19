using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookInstanceCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedOutDate",
                table: "BookInstances",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueDate",
                table: "BookInstances",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReaderId",
                table: "BookInstances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookInstances_ReaderId",
                table: "BookInstances",
                column: "ReaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookInstances_Readers_ReaderId",
                table: "BookInstances",
                column: "ReaderId",
                principalTable: "Readers",
                principalColumn: "ReaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookInstances_Readers_ReaderId",
                table: "BookInstances");

            migrationBuilder.DropIndex(
                name: "IX_BookInstances_ReaderId",
                table: "BookInstances");

            migrationBuilder.DropColumn(
                name: "CheckedOutDate",
                table: "BookInstances");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "BookInstances");

            migrationBuilder.DropColumn(
                name: "ReaderId",
                table: "BookInstances");
        }
    }
}
