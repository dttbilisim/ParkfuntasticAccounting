using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessTypeTransCodeSalesPersonIdToCashRegisterMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "ProcessType",
                table: "CashRegisterMovements",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<string>(
                name: "TransCode",
                table: "CashRegisterMovements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesPersonId",
                table: "CashRegisterMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterMovements_SalesPersonId",
                table: "CashRegisterMovements",
                column: "SalesPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashRegisterMovements_SalesPersons_SalesPersonId",
                table: "CashRegisterMovements",
                column: "SalesPersonId",
                principalTable: "SalesPersons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashRegisterMovements_SalesPersons_SalesPersonId",
                table: "CashRegisterMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisterMovements_SalesPersonId",
                table: "CashRegisterMovements");

            migrationBuilder.DropColumn(
                name: "ProcessType",
                table: "CashRegisterMovements");

            migrationBuilder.DropColumn(
                name: "TransCode",
                table: "CashRegisterMovements");

            migrationBuilder.DropColumn(
                name: "SalesPersonId",
                table: "CashRegisterMovements");
        }
    }
}
