using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <summary>CashRegisterMovements tablosuna TransCode ve ProcessType kolonlarını ekler (20260303130000 migration zincirde olmadığı için manuel ekleme).</summary>
    public partial class EnsureTransCodeProcessTypeInCashRegisterMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'CashRegisterMovements' AND column_name = 'TransCode') THEN
                        ALTER TABLE ""CashRegisterMovements"" ADD COLUMN ""TransCode"" character varying(50) NULL;
                    END IF;
                END $$;
            ");
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'CashRegisterMovements' AND column_name = 'ProcessType') THEN
                        ALTER TABLE ""CashRegisterMovements"" ADD COLUMN ""ProcessType"" smallint NOT NULL DEFAULT 1;
                    END IF;
                END $$;
            ");
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'CashRegisterMovements' AND column_name = 'SalesPersonId') THEN
                        ALTER TABLE ""CashRegisterMovements"" ADD COLUMN ""SalesPersonId"" integer NULL;
                    END IF;
                END $$;
            ");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_CashRegisterMovements_SalesPersonId"" ON ""CashRegisterMovements"" (""SalesPersonId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CashRegisterMovements_SalesPersonId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""CashRegisterMovements"" DROP COLUMN IF EXISTS ""SalesPersonId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""CashRegisterMovements"" DROP COLUMN IF EXISTS ""ProcessType"";");
            migrationBuilder.Sql(@"ALTER TABLE ""CashRegisterMovements"" DROP COLUMN IF EXISTS ""TransCode"";");
        }
    }
}
