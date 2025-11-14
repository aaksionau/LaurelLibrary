using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaurelLibrary.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningCenterToLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanningCenterApplicationId",
                table: "Libraries",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanningCenterSecret",
                table: "Libraries",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanningCenterApplicationId",
                table: "Libraries");

            migrationBuilder.DropColumn(
                name: "PlanningCenterSecret",
                table: "Libraries");
        }
    }
}
