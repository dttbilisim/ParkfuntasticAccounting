using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountIdToCashRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankAccountId",
                table: "CashRegisters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_BankAccountId",
                table: "CashRegisters",
                column: "BankAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_BankAccountId",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "CashRegisters");
        }
    }
}
