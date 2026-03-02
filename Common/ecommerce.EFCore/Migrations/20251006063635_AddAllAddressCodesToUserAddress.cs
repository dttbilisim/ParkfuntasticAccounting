using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAllAddressCodesToUserAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuildingCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NeighboorCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeighboorId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreetId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "HomeCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "HomeId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "NeighboorCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "NeighboorId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "StreetCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "StreetId",
                table: "UserAddresses");
        }
    }
}
