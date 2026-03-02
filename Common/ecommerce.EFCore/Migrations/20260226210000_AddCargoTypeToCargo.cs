using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <summary>
    /// Cargo tablosuna CargoType ekler (Standard=0, BicopsExpress=1).
    /// Hızlı Kargo Bicops Express için kullanılır. İleride depo bazlı kargo için genişletilebilir.
    /// </summary>
    public partial class AddCargoTypeToCargo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "CargoType",
                table: "Cargoes",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            // Hızlı Kargo Bicops Express kaydı — kurye teslimatı seçildiğinde kullanılır (scripts/seed_bicops_express_cargo.sql)
            migrationBuilder.Sql(@"
                INSERT INTO ""Cargoes"" (""Name"", ""Amount"", ""CargoOverloadPrice"", ""Message"", ""IsLocalStorage"", ""CargoType"", ""Status"", ""CreatedDate"", ""CreatedId"")
                SELECT 'Hızlı Kargo Bicops Express', 0, 0, 'Kurye teslimatı', false, 1, 1, NOW(), 1
                WHERE NOT EXISTS (SELECT 1 FROM ""Cargoes"" c WHERE c.""CargoType"" = 1);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoType",
                table: "Cargoes");
        }
    }
}
