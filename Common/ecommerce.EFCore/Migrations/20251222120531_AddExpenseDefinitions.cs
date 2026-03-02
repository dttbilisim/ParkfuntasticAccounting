using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "PriceLists",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenseDefinitions",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseDefinitions_ExpenseDefinitions_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ExpenseDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_CurrencyId",
                table: "PriceLists",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDefinitions_ParentId",
                table: "ExpenseDefinitions",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceLists_Currencies_CurrencyId",
                table: "PriceLists",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceLists_Currencies_CurrencyId",
                table: "PriceLists");

            migrationBuilder.DropTable(
                name: "ExpenseDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_PriceLists_CurrencyId",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "PriceLists");
        }
    }
}
