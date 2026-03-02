using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBankToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BankId",
                table: "Orders",
                column: "BankId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Banks_BankId",
                table: "Orders",
                column: "BankId",
                principalTable: "Banks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Banks_BankId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BankId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "Orders");
        }
    }
}
