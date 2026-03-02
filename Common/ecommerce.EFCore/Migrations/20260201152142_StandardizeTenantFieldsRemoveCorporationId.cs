using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeTenantFieldsRemoveCorporationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Corporations_CorporationId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BranchId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_CorporationId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "CashRegisters");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "PaymentTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "InvoiceTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ExpenseDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "InvoiceTypes");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ExpenseDefinitions");

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Warehouses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "CashRegisters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId",
                table: "Warehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_CorporationId",
                table: "Warehouses",
                column: "CorporationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Corporations_CorporationId",
                table: "Warehouses",
                column: "CorporationId",
                principalTable: "Corporations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
