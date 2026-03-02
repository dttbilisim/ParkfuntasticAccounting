using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceAddress",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceCityId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceTownId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSameAsDeliveryAddress",
                table: "UserAddresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_InvoiceCityId",
                table: "UserAddresses",
                column: "InvoiceCityId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_InvoiceTownId",
                table: "UserAddresses",
                column: "InvoiceTownId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_City_InvoiceCityId",
                table: "UserAddresses",
                column: "InvoiceCityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_Town_InvoiceTownId",
                table: "UserAddresses",
                column: "InvoiceTownId",
                principalTable: "Town",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_City_InvoiceCityId",
                table: "UserAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_Town_InvoiceTownId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_UserAddresses_InvoiceCityId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_UserAddresses_InvoiceTownId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "InvoiceAddress",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "InvoiceCityId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "InvoiceTownId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "IsSameAsDeliveryAddress",
                table: "UserAddresses");
        }
    }
}
