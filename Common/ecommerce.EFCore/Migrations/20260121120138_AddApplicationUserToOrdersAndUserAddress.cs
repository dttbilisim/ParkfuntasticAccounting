using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserToOrdersAndUserAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserAddresses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ApplicationUserId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_ApplicationUserId",
                table: "UserAddresses",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CompanyId",
                table: "Orders",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_CompanyId",
                table: "Orders",
                column: "CompanyId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_AspNetUsers_ApplicationUserId",
                table: "UserAddresses",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_CompanyId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_AspNetUsers_ApplicationUserId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_UserAddresses_ApplicationUserId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CompanyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "UserAddresses");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserAddresses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
