using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCityTownCodesToUserAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CityCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TownCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "TownCode",
                table: "UserAddresses");
        }
    }
}
