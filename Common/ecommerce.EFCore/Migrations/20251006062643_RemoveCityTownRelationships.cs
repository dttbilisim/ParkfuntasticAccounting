using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCityTownRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Foreign key constraints are already removed or don't exist
            // This migration is mainly for removing navigation properties from entities
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_CityId",
                table: "UserAddresses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_TownId",
                table: "UserAddresses",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_Town_CityId",
                table: "Town",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Membership_CityId",
                table: "Membership",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Membership_TownId",
                table: "Membership",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWareHouses_CityId",
                table: "CompanyWareHouses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyWareHouses_TownId",
                table: "CompanyWareHouses",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_CityId",
                table: "Company",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Company_TownId",
                table: "Company",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CityId",
                table: "AspNetUsers",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TownId",
                table: "AspNetUsers",
                column: "TownId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_City_CityId",
                table: "AspNetUsers",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Town_TownId",
                table: "AspNetUsers",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Company_City_CityId",
                table: "Company",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Company_Town_TownId",
                table: "Company",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyWareHouses_City_CityId",
                table: "CompanyWareHouses",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyWareHouses_Town_TownId",
                table: "CompanyWareHouses",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Membership_City_CityId",
                table: "Membership",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Membership_Town_TownId",
                table: "Membership",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Town_City_CityId",
                table: "Town",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_City_CityId",
                table: "UserAddresses",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_Town_TownId",
                table: "UserAddresses",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id");
        }
    }
}
