using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierVehiclesAndVehicleToServiceArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourierVehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourierId = table.Column<int>(type: "integer", nullable: false),
                    VehicleType = table.Column<byte>(type: "smallint", nullable: false),
                    LicensePlate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierVehicles_Couriers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "Couriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourierVehicles_CourierId",
                table: "CourierVehicles",
                column: "CourierId");

            migrationBuilder.AddColumn<int>(
                name: "CourierVehicleId",
                table: "CourierServiceAreas",
                type: "integer",
                nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_CourierServiceAreas_CourierId_CityId_TownId_NeighboorId",
                table: "CourierServiceAreas");

            migrationBuilder.DropIndex(
                name: "IX_CourierServiceAreas_CourierId_CityId_TownId",
                table: "CourierServiceAreas");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierVehicleId",
                table: "CourierServiceAreas",
                column: "CourierVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId_NeighboorId",
                table: "CourierServiceAreas",
                columns: new[] { "CourierId", "CourierVehicleId", "CityId", "TownId", "NeighboorId" },
                unique: true,
                filter: "\"NeighboorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId",
                table: "CourierServiceAreas",
                columns: new[] { "CourierId", "CourierVehicleId", "CityId", "TownId" },
                unique: true,
                filter: "\"NeighboorId\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierServiceAreas_CourierVehicles_CourierVehicleId",
                table: "CourierServiceAreas",
                column: "CourierVehicleId",
                principalTable: "CourierVehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierServiceAreas_CourierVehicles_CourierVehicleId",
                table: "CourierServiceAreas");

            migrationBuilder.DropIndex(
                name: "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId_NeighboorId",
                table: "CourierServiceAreas");

            migrationBuilder.DropIndex(
                name: "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId",
                table: "CourierServiceAreas");

            migrationBuilder.DropIndex(
                name: "IX_CourierServiceAreas_CourierVehicleId",
                table: "CourierServiceAreas");

            migrationBuilder.DropColumn(
                name: "CourierVehicleId",
                table: "CourierServiceAreas");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierId_CityId_TownId_NeighboorId",
                table: "CourierServiceAreas",
                columns: new[] { "CourierId", "CityId", "TownId", "NeighboorId" },
                unique: true,
                filter: "\"NeighboorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierId_CityId_TownId",
                table: "CourierServiceAreas",
                columns: new[] { "CourierId", "CityId", "TownId" },
                unique: true,
                filter: "\"NeighboorId\" IS NULL");

            migrationBuilder.DropTable(
                name: "CourierVehicles");
        }
    }
}
