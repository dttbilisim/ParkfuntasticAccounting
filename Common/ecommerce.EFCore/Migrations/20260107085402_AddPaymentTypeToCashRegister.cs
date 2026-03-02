using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTypeToCashRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentTypeId",
                table: "CashRegisters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_PaymentTypeId",
                table: "CashRegisters",
                column: "PaymentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashRegisters_PaymentTypes_PaymentTypeId",
                table: "CashRegisters",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashRegisters_PaymentTypes_PaymentTypeId",
                table: "CashRegisters");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_PaymentTypeId",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "PaymentTypeId",
                table: "CashRegisters");
        }
    }
}
