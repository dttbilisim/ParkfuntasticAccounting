using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankId = table.Column<int>(type: "integer", nullable: true),
                    SystemCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrencyId = table.Column<int>(type: "integer", nullable: true),
                    AccountCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BranchName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CardNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BankAccounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BankAccountExpenses",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankAccountId = table.Column<int>(type: "integer", nullable: false),
                    MainExpenseId = table.Column<int>(type: "integer", nullable: false),
                    SubExpenseId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccountExpenses_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankAccountExpenses_ExpenseDefinitions_MainExpenseId",
                        column: x => x.MainExpenseId,
                        principalTable: "ExpenseDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankAccountExpenses_ExpenseDefinitions_SubExpenseId",
                        column: x => x.SubExpenseId,
                        principalTable: "ExpenseDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BankAccountInstallments",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankAccountId = table.Column<int>(type: "integer", nullable: false),
                    Installment = table.Column<int>(type: "integer", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Note = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccountInstallments_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountExpenses_BankAccountId",
                table: "BankAccountExpenses",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountExpenses_MainExpenseId",
                table: "BankAccountExpenses",
                column: "MainExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountExpenses_SubExpenseId",
                table: "BankAccountExpenses",
                column: "SubExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountInstallments_BankAccountId",
                table: "BankAccountInstallments",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_BankId",
                table: "BankAccounts",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_CurrencyId",
                table: "BankAccounts",
                column: "CurrencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountExpenses");

            migrationBuilder.DropTable(
                name: "BankAccountInstallments");

            migrationBuilder.DropTable(
                name: "BankAccounts");
        }
    }
}
