using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDotVehicleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DotVehicleData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatECode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Container = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ConstructionTime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VehicleTypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ManufacturerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BaseModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VehicleType = table.Column<int>(type: "integer", nullable: true),
                    Manufacturer = table.Column<int>(type: "integer", nullable: true),
                    BaseModel = table.Column<int>(type: "integer", nullable: true),
                    SubModel = table.Column<int>(type: "integer", nullable: true),
                    OriginalPriceNet = table.Column<decimal>(type: "numeric", nullable: true),
                    OriginalPriceGross = table.Column<decimal>(type: "numeric", nullable: true),
                    PowerHp = table.Column<int>(type: "integer", nullable: true),
                    PowerKw = table.Column<int>(type: "integer", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    FuelMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GearboxType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Length = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotVehicleData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DotVehicleData_DatECode",
                table: "DotVehicleData",
                column: "DatECode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DotVehicleData");
        }
    }
}
