using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserIdToUserCars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApplicationUserId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_ApplicationUserId",
                table: "UserCars",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_AspNetUsers_ApplicationUserId",
                table: "UserCars",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_AspNetUsers_ApplicationUserId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_ApplicationUserId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "UserCars");
        }
    }
}
