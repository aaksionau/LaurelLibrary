using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutDurationToLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckoutDurationDays",
                table: "Libraries",
                type: "int",
                nullable: false,
                defaultValue: 14
            );

            // Update existing libraries to have 14 days checkout duration
            migrationBuilder.Sql(
                "UPDATE Libraries SET CheckoutDurationDays = 14 WHERE CheckoutDurationDays = 0"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CheckoutDurationDays", table: "Libraries");
        }
    }
}
