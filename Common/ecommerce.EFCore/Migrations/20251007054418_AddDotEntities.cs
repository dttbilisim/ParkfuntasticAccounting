using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDotEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DotTokenCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotTokenCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DotVehicleTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotVehicleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DotVehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Year = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Engine = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FuelType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DotVehicleTypeId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotVehicles_DotVehicleTypes_DotVehicleTypeId",
                        column: x => x.DotVehicleTypeId,
                        principalTable: "DotVehicleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DotParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NetPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    GrossPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Availability = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DatVehicleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DotVehicleId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotParts_DotVehicles_DotVehicleId",
                        column: x => x.DotVehicleId,
                        principalTable: "DotVehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DotParts_DotVehicleId",
                table: "DotParts",
                column: "DotVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DotParts_PartNumber",
                table: "DotParts",
                column: "PartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DotTokenCaches_Token",
                table: "DotTokenCaches",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_DotVehicles_DatId",
                table: "DotVehicles",
                column: "DatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DotVehicles_DotVehicleTypeId",
                table: "DotVehicles",
                column: "DotVehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DotVehicleTypes_DatId",
                table: "DotVehicleTypes",
                column: "DatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DotParts");

            migrationBuilder.DropTable(
                name: "DotTokenCaches");

            migrationBuilder.DropTable(
                name: "DotVehicles");

            migrationBuilder.DropTable(
                name: "DotVehicleTypes");
        }
    }
}
