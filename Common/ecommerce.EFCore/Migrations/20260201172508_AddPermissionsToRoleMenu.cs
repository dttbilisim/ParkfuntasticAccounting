using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionsToRoleMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanCreate",
                table: "RoleMenus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDelete",
                table: "RoleMenus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanEdit",
                table: "RoleMenus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanView",
                table: "RoleMenus",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanCreate",
                table: "RoleMenus");

            migrationBuilder.DropColumn(
                name: "CanDelete",
                table: "RoleMenus");

            migrationBuilder.DropColumn(
                name: "CanEdit",
                table: "RoleMenus");

            migrationBuilder.DropColumn(
                name: "CanView",
                table: "RoleMenus");
        }
    }
}
