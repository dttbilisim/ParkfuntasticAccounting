using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class EnsureCustomerAccountTransactionIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Veritabanında kolon yoksa ekle (20260303104314 migration atlanmış veya hata almış olabilir)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'CashRegisterMovements' AND column_name = 'CustomerAccountTransactionId') THEN
                        ALTER TABLE ""CashRegisterMovements"" ADD COLUMN ""CustomerAccountTransactionId"" integer NULL;
                    END IF;
                END $$;
            ");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_CashRegisterMovements_CustomerAccountTransactionId"" ON ""CashRegisterMovements"" (""CustomerAccountTransactionId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CashRegisterMovements_CustomerAccountTransactionId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""CashRegisterMovements"" DROP COLUMN IF EXISTS ""CustomerAccountTransactionId"";");
        }
    }
}
