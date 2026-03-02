using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrdersCreditCardToBankCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CreditCards_CreditCartId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CreditCartId",
                table: "Orders",
                newName: "BankCardId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_CreditCartId",
                table: "Orders",
                newName: "IX_Orders_BankCardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_BankCards_BankCardId",
                table: "Orders",
                column: "BankCardId",
                principalTable: "BankCards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_BankCards_BankCardId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "BankCardId",
                table: "Orders",
                newName: "CreditCartId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_BankCardId",
                table: "Orders",
                newName: "IX_Orders_CreditCartId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CreditCards_CreditCartId",
                table: "Orders",
                column: "CreditCartId",
                principalTable: "CreditCards",
                principalColumn: "Id");
        }
    }
}
