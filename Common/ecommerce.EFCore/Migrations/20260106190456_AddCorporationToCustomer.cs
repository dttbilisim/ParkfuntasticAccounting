using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCorporationToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TownId",
                table: "Warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId",
                table: "Warehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CityId",
                table: "Warehouses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CorporationId",
                table: "Warehouses",
                column: "CorporationId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TownId",
                table: "Warehouses",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CorporationId",
                table: "Customers",
                column: "CorporationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Corporations_CorporationId",
                table: "Customers",
                column: "CorporationId",
                principalTable: "Corporations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_City_CityId",
                table: "Warehouses",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Corporations_CorporationId",
                table: "Warehouses",
                column: "CorporationId",
                principalTable: "Corporations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Town_TownId",
                table: "Warehouses",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Corporations_CorporationId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_City_CityId",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Corporations_CorporationId",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Town_TownId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BranchId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_CityId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_CorporationId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_TownId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CorporationId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TownId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Customers");
        }
    }
}
