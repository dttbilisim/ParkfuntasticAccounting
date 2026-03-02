using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckIdToCustomerAccountTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckId",
                table: "CustomerAccountTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccountTransactions_CheckId",
                table: "CustomerAccountTransactions",
                column: "CheckId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerAccountTransactions_Checks_CheckId",
                table: "CustomerAccountTransactions",
                column: "CheckId",
                principalTable: "Checks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerAccountTransactions_Checks_CheckId",
                table: "CustomerAccountTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAccountTransactions_CheckId",
                table: "CustomerAccountTransactions");

            migrationBuilder.DropColumn(
                name: "CheckId",
                table: "CustomerAccountTransactions");
        }
    }
}
