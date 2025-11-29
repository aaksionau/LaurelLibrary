using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneNumberToReader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Readers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Readers");
        }
    }
}
