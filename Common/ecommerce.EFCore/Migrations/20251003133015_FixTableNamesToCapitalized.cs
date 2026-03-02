using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class FixTableNamesToCapitalized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_AspNetUsers_city_CityId",
            //     table: "AspNetUsers");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_AspNetUsers_town_TownId",
            //     table: "AspNetUsers");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Company_city_CityId",
            //     table: "Company");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Company_town_TownId",
            //     table: "Company");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_CompanyWareHouses_city_CityId",
            //     table: "CompanyWareHouses");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_CompanyWareHouses_town_TownId",
            //     table: "CompanyWareHouses");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Membership_city_CityId",
            //     table: "Membership");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Membership_town_TownId",
            //     table: "Membership");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_neighboors_town_TownId",
            //     table: "neighboors");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_town_city_CityId",
            //     table: "town");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_UserAddresses_city_CityId",
            //     table: "UserAddresses");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_UserAddresses_town_TownId",
            //     table: "UserAddresses");

            // migrationBuilder.DropPrimaryKey(
            //     name: "PK_town",
            //     table: "town");

            // migrationBuilder.DropPrimaryKey(
            //     name: "PK_city",
            //     table: "city");

            // migrationBuilder.RenameTable(
            //     name: "town",
            //     newName: "Town");

            // migrationBuilder.RenameTable(
            //     name: "city",
            //     newName: "City");

            // migrationBuilder.RenameIndex(
            //     name: "IX_town_CityId",
            //     table: "Town",
            //     newName: "IX_Town_CityId");

            // migrationBuilder.AddPrimaryKey(
            //     name: "PK_Town",
            //     table: "Town",
            //     column: "Id");

            // migrationBuilder.AddPrimaryKey(
            //     name: "PK_City",
            //     table: "City",
            //     column: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_AspNetUsers_City_CityId",
            //     table: "AspNetUsers",
            //     column: "CityId",
            //     principalTable: "City",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_AspNetUsers_Town_TownId",
            //     table: "AspNetUsers",
            //     column: "TownId",
            //     principalTable: "Town",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Company_City_CityId",
            //     table: "Company",
            //     column: "CityId",
            //     principalTable: "City",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Company_Town_TownId",
            //     table: "Company",
            //     column: "TownId",
            //     principalTable: "Town",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_CompanyWareHouses_City_CityId",
            //     table: "CompanyWareHouses",
            //     column: "CityId",
            //     principalTable: "City",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_CompanyWareHouses_Town_TownId",
            //     table: "CompanyWareHouses",
            //     column: "TownId",
            //     principalTable: "Town",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Membership_City_CityId",
            //     table: "Membership",
            //     column: "CityId",
            //     principalTable: "City",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Membership_Town_TownId",
            //     table: "Membership",
            //     column: "TownId",
            //     principalTable: "Town",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_neighboors_Town_TownId",
            //     table: "neighboors",
            //     column: "TownId",
            //     principalTable: "Town",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Town_City_CityId",
            //     table: "Town",
            //     column: "CityId",
            //     principalTable: "City",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_UserAddresses_City_CityId",
            //     table: "UserAddresses",
            //     column: "CityId",
            //     principalTable: "City",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_UserAddresses_Town_TownId",
            //     table: "UserAddresses",
            //     column: "TownId",
            //     principalTable: "Town",
            //     principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_City_CityId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Town_TownId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Company_City_CityId",
                table: "Company");

            migrationBuilder.DropForeignKey(
                name: "FK_Company_Town_TownId",
                table: "Company");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyWareHouses_City_CityId",
                table: "CompanyWareHouses");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyWareHouses_Town_TownId",
                table: "CompanyWareHouses");

            migrationBuilder.DropForeignKey(
                name: "FK_Membership_City_CityId",
                table: "Membership");

            migrationBuilder.DropForeignKey(
                name: "FK_Membership_Town_TownId",
                table: "Membership");

            migrationBuilder.DropForeignKey(
                name: "FK_neighboors_Town_TownId",
                table: "neighboors");

            migrationBuilder.DropForeignKey(
                name: "FK_Town_City_CityId",
                table: "Town");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_City_CityId",
                table: "UserAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_Town_TownId",
                table: "UserAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Town",
                table: "Town");

            migrationBuilder.DropPrimaryKey(
                name: "PK_City",
                table: "City");

            migrationBuilder.RenameTable(
                name: "Town",
                newName: "town");

            migrationBuilder.RenameTable(
                name: "City",
                newName: "city");

            migrationBuilder.RenameIndex(
                name: "IX_Town_CityId",
                table: "town",
                newName: "IX_town_CityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_town",
                table: "town",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_city",
                table: "city",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_city_CityId",
                table: "AspNetUsers",
                column: "CityId",
                principalTable: "city",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_town_TownId",
                table: "AspNetUsers",
                column: "TownId",
                principalTable: "town",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Company_city_CityId",
                table: "Company",
                column: "CityId",
                principalTable: "city",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Company_town_TownId",
                table: "Company",
                column: "TownId",
                principalTable: "town",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyWareHouses_city_CityId",
                table: "CompanyWareHouses",
                column: "CityId",
                principalTable: "city",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyWareHouses_town_TownId",
                table: "CompanyWareHouses",
                column: "TownId",
                principalTable: "town",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Membership_city_CityId",
                table: "Membership",
                column: "CityId",
                principalTable: "city",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Membership_town_TownId",
                table: "Membership",
                column: "TownId",
                principalTable: "town",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_neighboors_town_TownId",
                table: "neighboors",
                column: "TownId",
                principalTable: "town",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_town_city_CityId",
                table: "town",
                column: "CityId",
                principalTable: "city",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_city_CityId",
                table: "UserAddresses",
                column: "CityId",
                principalTable: "city",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_town_TownId",
                table: "UserAddresses",
                column: "TownId",
                principalTable: "town",
                principalColumn: "Id");
        }
    }
}
