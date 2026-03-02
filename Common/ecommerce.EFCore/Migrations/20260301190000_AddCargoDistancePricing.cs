using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <summary>
    /// CargoType=BicopsExpress (1) için mesafe bazlı ücretlendirme: CoveredKm (kapsanan km), PricePerExtraKm (ek km başı ücret).
    /// </summary>
    public partial class AddCargoDistancePricing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CoveredKm",
                table: "Cargoes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerExtraKm",
                table: "Cargoes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoveredKm",
                table: "Cargoes");

            migrationBuilder.DropColumn(
                name: "PricePerExtraKm",
                table: "Cargoes");
        }
    }
}
