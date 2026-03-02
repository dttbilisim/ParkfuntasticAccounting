using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxFieldsToSeller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Sellers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxOffice",
                table: "Sellers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "TaxOffice",
                table: "Sellers");
        }
    }
}
