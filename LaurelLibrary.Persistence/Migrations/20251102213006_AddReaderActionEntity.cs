using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReaderActionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReaderActions",
                columns: table => new
                {
                    ReaderActionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReaderId = table.Column<int>(type: "int", nullable: false),
                    BookInstanceId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BookTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BookIsbn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BookAuthors = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LibraryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaderActions", x => x.ReaderActionId);
                    table.ForeignKey(
                        name: "FK_ReaderActions_BookInstances_BookInstanceId",
                        column: x => x.BookInstanceId,
                        principalTable: "BookInstances",
                        principalColumn: "BookInstanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReaderActions_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "LibraryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReaderActions_Readers_ReaderId",
                        column: x => x.ReaderId,
                        principalTable: "Readers",
                        principalColumn: "ReaderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReaderActions_BookInstanceId",
                table: "ReaderActions",
                column: "BookInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaderActions_LibraryId",
                table: "ReaderActions",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReaderActions_ReaderId",
                table: "ReaderActions",
                column: "ReaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReaderActions");
        }
    }
}
