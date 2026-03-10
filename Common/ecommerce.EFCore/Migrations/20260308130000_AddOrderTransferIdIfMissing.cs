using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTransferIdIfMissing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL: OrderTransferId sütunu yoksa ekle
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Orders' AND column_name = 'OrderTransferId') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""OrderTransferId"" integer NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down'da sütunu kaldırma - veri kaybı olabilir
            // Gerekirse: migrationBuilder.DropColumn(name: "OrderTransferId", table: "Orders");
        }
    }
}
