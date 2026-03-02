using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddParaBirimiToProductOtoIsmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Users_UserId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MyFavorites_Users_UserId",
                table: "MyFavorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_CompanyId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_Users_UserId",
                table: "UserAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_Users_UserId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_UserId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserAddresses_UserId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CompanyId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "ProductOtoIsmails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "ProductOtoIsmails");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_UserId",
                table: "UserCars",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_UserId",
                table: "UserAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CompanyId",
                table: "Orders",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Users_UserId",
                table: "CartItems",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MyFavorites_Users_UserId",
                table: "MyFavorites",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_CompanyId",
                table: "Orders",
                column: "CompanyId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_Users_UserId",
                table: "UserAddresses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_Users_UserId",
                table: "UserCars",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
