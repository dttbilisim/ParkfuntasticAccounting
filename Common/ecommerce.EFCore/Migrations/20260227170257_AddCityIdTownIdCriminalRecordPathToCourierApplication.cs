using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCityIdTownIdCriminalRecordPathToCourierApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "CourierApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriminalRecordPath",
                table: "CourierApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TownId",
                table: "CourierApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourierApplications_CityId",
                table: "CourierApplications",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierApplications_TownId",
                table: "CourierApplications",
                column: "TownId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierApplications_City_CityId",
                table: "CourierApplications",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierApplications_Town_TownId",
                table: "CourierApplications",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourierApplications_City_CityId",
                table: "CourierApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierApplications_Town_TownId",
                table: "CourierApplications");

            migrationBuilder.DropIndex(
                name: "IX_CourierApplications_CityId",
                table: "CourierApplications");

            migrationBuilder.DropIndex(
                name: "IX_CourierApplications_TownId",
                table: "CourierApplications");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "CourierApplications");

            migrationBuilder.DropColumn(
                name: "CriminalRecordPath",
                table: "CourierApplications");

            migrationBuilder.DropColumn(
                name: "TownId",
                table: "CourierApplications");
        }
    }
}
