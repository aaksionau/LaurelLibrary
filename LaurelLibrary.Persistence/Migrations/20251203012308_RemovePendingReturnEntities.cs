using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePendingReturnEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingReturnItems");

            migrationBuilder.DropTable(
                name: "PendingReturns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingReturns",
                columns: table => new
                {
                    PendingReturnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LibraryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReaderId = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingReturns", x => x.PendingReturnId);
                    table.ForeignKey(
                        name: "FK_PendingReturns_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "LibraryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingReturns_Readers_ReaderId",
                        column: x => x.ReaderId,
                        principalTable: "Readers",
                        principalColumn: "ReaderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingReturnItems",
                columns: table => new
                {
                    PendingReturnItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookInstanceId = table.Column<int>(type: "int", nullable: false),
                    PendingReturnId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingReturnItems", x => x.PendingReturnItemId);
                    table.ForeignKey(
                        name: "FK_PendingReturnItems_BookInstances_BookInstanceId",
                        column: x => x.BookInstanceId,
                        principalTable: "BookInstances",
                        principalColumn: "BookInstanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingReturnItems_PendingReturns_PendingReturnId",
                        column: x => x.PendingReturnId,
                        principalTable: "PendingReturns",
                        principalColumn: "PendingReturnId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingReturnItems_BookInstanceId",
                table: "PendingReturnItems",
                column: "BookInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingReturnItems_PendingReturnId",
                table: "PendingReturnItems",
                column: "PendingReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingReturns_LibraryId",
                table: "PendingReturns",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingReturns_ReaderId",
                table: "PendingReturns",
                column: "ReaderId");
        }
    }
}
