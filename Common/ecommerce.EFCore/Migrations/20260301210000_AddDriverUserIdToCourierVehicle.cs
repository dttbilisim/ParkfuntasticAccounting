using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <summary>
    /// Araç-şoför ilişkisi: CourierVehicles tablosuna DriverUserId (alt kullanıcı) ekler.
    /// </summary>
    public partial class AddDriverUserIdToCourierVehicle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverUserId",
                table: "CourierVehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourierVehicles_DriverUserId",
                table: "CourierVehicles",
                column: "DriverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierVehicles_AspNetUsers_DriverUserId",
                table: "CourierVehicles",
                column: "DriverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierVehicles_AspNetUsers_DriverUserId",
                table: "CourierVehicles");

            migrationBuilder.DropIndex(
                name: "IX_CourierVehicles_DriverUserId",
                table: "CourierVehicles");

            migrationBuilder.DropColumn(
                name: "DriverUserId",
                table: "CourierVehicles");
        }
    }
}
