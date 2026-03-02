using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <summary>
    /// PostgreSQL'de WorkEndTime/WorkStartTime bazen küçük harf (workendtime/workstarttime) oluşuyor veya hiç yok.
    /// Entity [Column("workstarttime")] ile eşlendiği için bu sütunları ekliyoruz (yoksa).
    /// </summary>
    public partial class EnsureCourierServiceAreaWorkingHoursColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""CourierServiceAreas"" ADD COLUMN IF NOT EXISTS workstarttime time NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""CourierServiceAreas"" ADD COLUMN IF NOT EXISTS workendtime time NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""CourierServiceAreas"" DROP COLUMN IF EXISTS workstarttime;");
            migrationBuilder.Sql(@"ALTER TABLE ""CourierServiceAreas"" DROP COLUMN IF EXISTS workendtime;");
        }
    }
}
