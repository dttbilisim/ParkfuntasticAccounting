using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDotVehicleImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DotVehicleImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatECode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aspect = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ImageType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ImageFormat = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ImageBase64 = table.Column<string>(type: "text", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotVehicleImages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DotVehicleImages");
        }
    }
}
