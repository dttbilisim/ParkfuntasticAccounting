using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvoiceCurrencyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Currencies_CurrencyId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Invoices");

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Currencies_CurrencyId",
                table: "Invoices",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Currencies_CurrencyId",
                table: "Invoices");

            migrationBuilder.AlterColumn<int>(
                name: "CurrencyId",
                table: "Invoices",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Invoices",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Currencies_CurrencyId",
                table: "Invoices",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }
    }
}
