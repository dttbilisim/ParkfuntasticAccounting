using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUnitIdToInvoiceItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductUnitId",
                table: "InvoiceItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ProductUnitId",
                table: "InvoiceItems",
                column: "ProductUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_ProductUnits_ProductUnitId",
                table: "InvoiceItems",
                column: "ProductUnitId",
                principalTable: "ProductUnits",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_ProductUnits_ProductUnitId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_ProductUnitId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ProductUnitId",
                table: "InvoiceItems");
        }
    }
}
