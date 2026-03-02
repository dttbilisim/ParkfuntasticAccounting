using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOemDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductOemDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Oem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DatProcessNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NetPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    VehicleType = table.Column<int>(type: "integer", nullable: true),
                    VehicleTypeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ManufacturerKey = table.Column<int>(type: "integer", nullable: true),
                    ManufacturerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BaseModelKey = table.Column<int>(type: "integer", nullable: true),
                    BaseModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubModelsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOemDetail", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductOemDetail");
        }
    }
}
