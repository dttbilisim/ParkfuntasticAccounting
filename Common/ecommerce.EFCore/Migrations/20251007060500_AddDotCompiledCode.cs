using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDotCompiledCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DotCompiledCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatECode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VehicleType = table.Column<int>(type: "integer", nullable: false),
                    ManufacturerKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseModelKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubModelKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SelectedOptions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotCompiledCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DotCompiledCodes_DatECode",
                table: "DotCompiledCodes",
                column: "DatECode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DotCompiledCodes");
        }
    }
}
