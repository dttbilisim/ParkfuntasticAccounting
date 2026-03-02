using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <summary>
    /// Siparişe atanan kurye aracı: Hangi araç/şoför ile teslim edileceği; kargo takip listesinde doğru isim/plaka gösterimi için.
    /// </summary>
    public partial class AddCourierVehicleIdToOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourierVehicleId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CourierVehicleId",
                table: "Orders",
                column: "CourierVehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CourierVehicles_CourierVehicleId",
                table: "Orders",
                column: "CourierVehicleId",
                principalTable: "CourierVehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CourierVehicles_CourierVehicleId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CourierVehicleId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CourierVehicleId",
                table: "Orders");
        }
    }
}
