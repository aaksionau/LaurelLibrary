using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoApproveMobileReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoApproveMobileReturns",
                table: "Libraries",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoApproveMobileReturns",
                table: "Libraries");
        }
    }
}
