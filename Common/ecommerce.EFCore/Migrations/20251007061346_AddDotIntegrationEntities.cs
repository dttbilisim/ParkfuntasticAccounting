using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDotIntegrationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DotCarBodyOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VehicleType = table.Column<int>(type: "integer", nullable: false),
                    ManufacturerKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseModelKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubModelKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotCarBodyOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DotEngineOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VehicleType = table.Column<int>(type: "integer", nullable: false),
                    ManufacturerKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseModelKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubModelKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotEngineOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DotOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VehicleType = table.Column<int>(type: "integer", nullable: false),
                    ManufacturerKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseModelKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubModelKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Classification = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotOptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DotCarBodyOptions_DatKey_VehicleType_ManufacturerKey_BaseMo~",
                table: "DotCarBodyOptions",
                columns: new[] { "DatKey", "VehicleType", "ManufacturerKey", "BaseModelKey", "SubModelKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DotEngineOptions_DatKey_VehicleType_ManufacturerKey_BaseMod~",
                table: "DotEngineOptions",
                columns: new[] { "DatKey", "VehicleType", "ManufacturerKey", "BaseModelKey", "SubModelKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DotOptions_DatKey_VehicleType_ManufacturerKey_BaseModelKey_~",
                table: "DotOptions",
                columns: new[] { "DatKey", "VehicleType", "ManufacturerKey", "BaseModelKey", "SubModelKey", "Classification" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DotCarBodyOptions");

            migrationBuilder.DropTable(
                name: "DotEngineOptions");

            migrationBuilder.DropTable(
                name: "DotOptions");
        }
    }
}
