using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class SyncSnapshotWithModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns only if they don't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Sellers' AND column_name = 'IsOrderUse') THEN
                        ALTER TABLE ""Sellers"" ADD COLUMN ""IsOrderUse"" boolean NOT NULL DEFAULT FALSE;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Sellers' AND column_name = 'TaxNumber') THEN
                        ALTER TABLE ""Sellers"" ADD COLUMN ""TaxNumber"" text;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Sellers' AND column_name = 'TaxOffice') THEN
                        ALTER TABLE ""Sellers"" ADD COLUMN ""TaxOffice"" text;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Product' AND column_name = 'IsStockFollow') THEN
                        ALTER TABLE ""Product"" ADD COLUMN ""IsStockFollow"" boolean NOT NULL DEFAULT FALSE;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Sellers' AND column_name = 'IsOrderUse') THEN
                        ALTER TABLE ""Sellers"" DROP COLUMN ""IsOrderUse"";
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Sellers' AND column_name = 'TaxNumber') THEN
                        ALTER TABLE ""Sellers"" DROP COLUMN ""TaxNumber"";
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Sellers' AND column_name = 'TaxOffice') THEN
                        ALTER TABLE ""Sellers"" DROP COLUMN ""TaxOffice"";
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Product' AND column_name = 'IsStockFollow') THEN
                        ALTER TABLE ""Product"" DROP COLUMN ""IsStockFollow"";
                    END IF;
                END $$;
            ");
        }
    }
}
