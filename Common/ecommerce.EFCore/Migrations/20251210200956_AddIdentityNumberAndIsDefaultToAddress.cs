using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityNumberAndIsDefaultToAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TcKimlikNo",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "IdentityNumber",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "UserAddresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityNumber",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "UserAddresses");

            migrationBuilder.AddColumn<string>(
                name: "TcKimlikNo",
                table: "Users",
                type: "text",
                nullable: true);
        }
    }
}
