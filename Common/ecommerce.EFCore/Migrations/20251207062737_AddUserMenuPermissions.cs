using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMenuPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanCreate",
                table: "UserMenus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDelete",
                table: "UserMenus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanEdit",
                table: "UserMenus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanView",
                table: "UserMenus",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanCreate",
                table: "UserMenus");

            migrationBuilder.DropColumn(
                name: "CanDelete",
                table: "UserMenus");

            migrationBuilder.DropColumn(
                name: "CanEdit",
                table: "UserMenus");

            migrationBuilder.DropColumn(
                name: "CanView",
                table: "UserMenus");
        }
    }
}
