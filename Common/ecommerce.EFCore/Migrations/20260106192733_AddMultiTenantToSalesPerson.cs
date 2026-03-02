using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantToSalesPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "SalesPersons");

            migrationBuilder.DropColumn(
                name: "Town",
                table: "SalesPersons");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SalesPersons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "SalesPersons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "SalesPersons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TownId",
                table: "SalesPersons",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesPersons_BranchId",
                table: "SalesPersons",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPersons_CityId",
                table: "SalesPersons",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPersons_CorporationId",
                table: "SalesPersons",
                column: "CorporationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPersons_TownId",
                table: "SalesPersons",
                column: "TownId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_Branches_BranchId",
                table: "SalesPersons",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_City_CityId",
                table: "SalesPersons",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_Corporations_CorporationId",
                table: "SalesPersons",
                column: "CorporationId",
                principalTable: "Corporations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_Town_TownId",
                table: "SalesPersons",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_Branches_BranchId",
                table: "SalesPersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_City_CityId",
                table: "SalesPersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_Corporations_CorporationId",
                table: "SalesPersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_Town_TownId",
                table: "SalesPersons");

            migrationBuilder.DropIndex(
                name: "IX_SalesPersons_BranchId",
                table: "SalesPersons");

            migrationBuilder.DropIndex(
                name: "IX_SalesPersons_CityId",
                table: "SalesPersons");

            migrationBuilder.DropIndex(
                name: "IX_SalesPersons_CorporationId",
                table: "SalesPersons");

            migrationBuilder.DropIndex(
                name: "IX_SalesPersons_TownId",
                table: "SalesPersons");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SalesPersons");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "SalesPersons");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "SalesPersons");

            migrationBuilder.DropColumn(
                name: "TownId",
                table: "SalesPersons");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "SalesPersons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Town",
                table: "SalesPersons",
                type: "text",
                nullable: true);
        }
    }
}
