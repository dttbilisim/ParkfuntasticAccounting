using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddIsStaticToCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStatic",
                table: "Currencies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStatic",
                table: "Currencies");
        }
    }
}
