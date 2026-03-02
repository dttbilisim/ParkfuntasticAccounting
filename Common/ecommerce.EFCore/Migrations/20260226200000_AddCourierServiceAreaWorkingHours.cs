using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierServiceAreaWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "WorkStartTime",
                table: "CourierServiceAreas",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WorkEndTime",
                table: "CourierServiceAreas",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkStartTime",
                table: "CourierServiceAreas");

            migrationBuilder.DropColumn(
                name: "WorkEndTime",
                table: "CourierServiceAreas");
        }
    }
}
