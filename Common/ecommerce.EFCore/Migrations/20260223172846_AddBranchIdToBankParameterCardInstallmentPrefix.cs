using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToBankParameterCardInstallmentPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "BankParameters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "BankCreditCardPrefixs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "BankCreditCardInstallments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "BankCards",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankParameters");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankCreditCardPrefixs");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankCreditCardInstallments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankCards");
        }
    }
}
