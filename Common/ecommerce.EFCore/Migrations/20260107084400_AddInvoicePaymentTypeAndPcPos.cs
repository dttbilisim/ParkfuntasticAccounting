using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentTypeAndPcPos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PcPosDefinitionId",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentTypes",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsCash = table.Column<bool>(type: "boolean", nullable: false),
                    IsCreditCard = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CashRegisterId",
                table: "Invoices",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentTypeId",
                table: "Invoices",
                column: "PaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PcPosDefinitionId",
                table: "Invoices",
                column: "PcPosDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_CashRegisters_CashRegisterId",
                table: "Invoices",
                column: "CashRegisterId",
                principalTable: "CashRegisters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_PaymentTypes_PaymentTypeId",
                table: "Invoices",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_PcPosDefinitions_PcPosDefinitionId",
                table: "Invoices",
                column: "PcPosDefinitionId",
                principalTable: "PcPosDefinitions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_CashRegisters_CashRegisterId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_PaymentTypes_PaymentTypeId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_PcPosDefinitions_PcPosDefinitionId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "PaymentTypes");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CashRegisterId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PaymentTypeId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PcPosDefinitionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PcPosDefinitionId",
                table: "Invoices");
        }
    }
}
