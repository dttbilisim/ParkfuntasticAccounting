using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCorporationIdFromSalesPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_Corporations_CorporationId",
                table: "SalesPersons");

            migrationBuilder.DropIndex(
                name: "IX_SalesPersons_CorporationId",
                table: "SalesPersons");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "SalesPersons");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Units",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Tax",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "BankAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Tax");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankAccounts");

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "SalesPersons",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesPersons_CorporationId",
                table: "SalesPersons",
                column: "CorporationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_Corporations_CorporationId",
                table: "SalesPersons",
                column: "CorporationId",
                principalTable: "Corporations",
                principalColumn: "Id");
        }
    }
}
