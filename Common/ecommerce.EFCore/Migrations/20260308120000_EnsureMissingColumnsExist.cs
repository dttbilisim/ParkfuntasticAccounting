using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class EnsureMissingColumnsExist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL: Sütunlar yoksa ekle (migration zaten uygulanmış görünse bile veritabanında eksik olabilir)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Invoices' AND column_name = 'IsTrans') THEN
                        ALTER TABLE ""Invoices"" ADD COLUMN ""IsTrans"" boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;
            ");
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Orders' AND column_name = 'GuideName') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""GuideName"" text NULL;
                    END IF;
                END $$;
            ");
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Orders' AND column_name = 'Voucher') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""Voucher"" text NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down'da sütunları kaldırmıyoruz - zaten mevcut migration'ların Down'ı var
            // Bu migration sadece eksik sütunları tamamlamak için
        }
    }
}
