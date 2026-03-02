using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductOtoIsmailPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi1",
                table: "ProductOtoIsmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi3",
                table: "ProductOtoIsmails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParaBirimi1",
                table: "ProductOtoIsmails");

            migrationBuilder.DropColumn(
                name: "ParaBirimi3",
                table: "ProductOtoIsmails");
        }
    }
}
