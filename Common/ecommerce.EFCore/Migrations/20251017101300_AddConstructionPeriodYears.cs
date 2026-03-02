using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructionPeriodYears : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YearMax",
                table: "DotConstructionPeriods",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearMin",
                table: "DotConstructionPeriods",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YearMax",
                table: "DotConstructionPeriods");

            migrationBuilder.DropColumn(
                name: "YearMin",
                table: "DotConstructionPeriods");
        }
    }
}
