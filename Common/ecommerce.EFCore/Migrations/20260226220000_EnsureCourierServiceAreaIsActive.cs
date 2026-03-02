using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <summary>
    /// CourierServiceAreas tablosunda IsActive sütunu yoksa ekler.
    /// Npgsql 42703: column c0.IsActive does not exist hatasını giderir.
    /// </summary>
    public partial class EnsureCourierServiceAreaIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""CourierServiceAreas"" ADD COLUMN IF NOT EXISTS ""IsActive"" boolean NOT NULL DEFAULT true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""CourierServiceAreas"" DROP COLUMN IF EXISTS ""IsActive"";");
        }
    }
}
