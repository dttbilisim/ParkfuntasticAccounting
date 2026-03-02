using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTypeToPcPosDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentTypeId",
                table: "PcPosDefinitions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PcPosDefinitions_PaymentTypeId",
                table: "PcPosDefinitions",
                column: "PaymentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PcPosDefinitions_PaymentTypes_PaymentTypeId",
                table: "PcPosDefinitions",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PcPosDefinitions_PaymentTypes_PaymentTypeId",
                table: "PcPosDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_PcPosDefinitions_PaymentTypeId",
                table: "PcPosDefinitions");

            migrationBuilder.DropColumn(
                name: "PaymentTypeId",
                table: "PcPosDefinitions");
        }
    }
}
