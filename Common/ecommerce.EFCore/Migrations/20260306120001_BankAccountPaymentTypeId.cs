using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class BankAccountPaymentTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentTypeId",
                table: "BankAccounts",
                type: "integer",
                nullable: true);

            // Migrate: Map enum (1=Nakit, 2=KrediKarti, 3=HavaleEFT, 4=Cek) to PaymentType.Id by name
            migrationBuilder.Sql(@"
                UPDATE ""BankAccounts"" ba
                SET ""PaymentTypeId"" = (
                    SELECT pt.""Id"" FROM ""PaymentTypes"" pt
                    WHERE pt.""Status"" != 99
                    AND (
                        (ba.""PaymentType"" = 1 AND (pt.""Name"" ILIKE '%Nakit%' OR pt.""Name"" ILIKE 'Nakit'))
                        OR (ba.""PaymentType"" = 2 AND (pt.""Name"" ILIKE '%Kredi%' OR pt.""Name"" ILIKE '%Kart%'))
                        OR (ba.""PaymentType"" = 3 AND (pt.""Name"" ILIKE '%Havale%' OR pt.""Name"" ILIKE '%EFT%'))
                        OR (ba.""PaymentType"" = 4 AND (pt.""Name"" ILIKE '%Çek%' OR pt.""Name"" ILIKE 'Cek'))
                    )
                    LIMIT 1
                )
            ");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "BankAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_PaymentTypeId",
                table: "BankAccounts",
                column: "PaymentTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankAccounts_PaymentTypeId",
                table: "BankAccounts");

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "BankAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
                UPDATE ""BankAccounts"" ba
                SET ""PaymentType"" = CASE
                    WHEN pt.""Name"" ILIKE '%Nakit%' THEN 1
                    WHEN pt.""Name"" ILIKE '%Kredi%' OR pt.""Name"" ILIKE '%Kart%' THEN 2
                    WHEN pt.""Name"" ILIKE '%Havale%' OR pt.""Name"" ILIKE '%EFT%' THEN 3
                    WHEN pt.""Name"" ILIKE '%Çek%' OR pt.""Name"" ILIKE 'Cek' THEN 4
                    ELSE 1
                END
                FROM ""PaymentTypes"" pt
                WHERE ba.""PaymentTypeId"" = pt.""Id""
            ");

            migrationBuilder.DropColumn(
                name: "PaymentTypeId",
                table: "BankAccounts");
        }
    }
}
