using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Regions");

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_RegionId",
                table: "Customers",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Regions_RegionId",
                table: "Customers",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Regions_RegionId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_RegionId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Regions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
