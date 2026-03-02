using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTownIdToNeighboors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_addressinfos_homes_HomeId",
            //     table: "addressinfos");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_buildings_streets_StreetId",
            //     table: "buildings");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_homes_buildings_BuildingId",
            //     table: "homes");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_neighboors_Town_TownId",
            //     table: "neighboors");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_streets_neighboors_NeighboorId",
            //     table: "streets");

            // migrationBuilder.DropPrimaryKey(
            //     name: "PK_streets",
            //     table: "streets");

            // migrationBuilder.DropPrimaryKey(
            //     name: "PK_neighboors",
            //     table: "neighboors");

            // migrationBuilder.DropPrimaryKey(
            //     name: "PK_homes",
            //     table: "homes");

            // migrationBuilder.DropPrimaryKey(
            //     name: "PK_buildings",
            //     table: "buildings");

            // migrationBuilder.DropPrimaryKey(
            //     name: "PK_addressinfos",
            //     table: "addressinfos");

            // migrationBuilder.RenameTable(
            //     name: "streets",
            //     newName: "Streets");

            // migrationBuilder.RenameTable(
            //     name: "neighboors",
            //     newName: "Neighboors");

            // migrationBuilder.RenameTable(
            //     name: "homes",
            //     newName: "Homes");

            // migrationBuilder.RenameTable(
            //     name: "buildings",
            //     newName: "Buildings");

            // migrationBuilder.RenameTable(
            //     name: "addressinfos",
            //     newName: "Addressinfos");

            // migrationBuilder.RenameIndex(
            //     name: "IX_streets_NeighboorId",
            //     table: "Streets",
            //     newName: "IX_Streets_NeighboorId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_neighboors_TownId",
            //     table: "Neighboors",
            //     newName: "IX_Neighboors_TownId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_homes_BuildingId",
            //     table: "Homes",
            //     newName: "IX_Homes_BuildingId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_buildings_StreetId",
            //     table: "Buildings",
            //     newName: "IX_Buildings_StreetId");

            // migrationBuilder.RenameIndex(
            //     name: "IX_addressinfos_HomeId",
            //     table: "Addressinfos",
            //     newName: "IX_Addressinfos_HomeId");

            // migrationBuilder.AddPrimaryKey(
            //     name: "PK_Streets",
            //     table: "Streets",
            //     column: "Id");

            // migrationBuilder.AddPrimaryKey(
            //     name: "PK_Neighboors",
            //     table: "Neighboors",
            //     column: "Id");

            // migrationBuilder.AddPrimaryKey(
            //     name: "PK_Homes",
            //     table: "Homes",
            //     column: "Id");

            // migrationBuilder.AddPrimaryKey(
            //     name: "PK_Buildings",
            //     table: "Buildings",
            //     column: "Id");

            // migrationBuilder.AddPrimaryKey(
            //     name: "PK_Addressinfos",
            //     table: "Addressinfos",
            //     column: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Addressinfos_Homes_HomeId",
            //     table: "Addressinfos",
            //     column: "HomeId",
            //     principalTable: "Homes",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Buildings_Streets_StreetId",
            //     table: "Buildings",
            //     column: "StreetId",
            //     principalTable: "Streets",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Homes_Buildings_BuildingId",
            //     table: "Homes",
            //     column: "BuildingId",
            //     principalTable: "Buildings",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Neighboors_Town_TownId",
            //     table: "Neighboors",
            //     column: "TownId",
            //     principalTable: "Town",
            //     principalColumn: "Id");

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Streets_Neighboors_NeighboorId",
            //     table: "Streets",
            //     column: "NeighboorId",
            //     principalTable: "Neighboors",
            //     principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addressinfos_Homes_HomeId",
                table: "Addressinfos");

            migrationBuilder.DropForeignKey(
                name: "FK_Buildings_Streets_StreetId",
                table: "Buildings");

            migrationBuilder.DropForeignKey(
                name: "FK_Homes_Buildings_BuildingId",
                table: "Homes");

            migrationBuilder.DropForeignKey(
                name: "FK_Neighboors_Town_TownId",
                table: "Neighboors");

            migrationBuilder.DropForeignKey(
                name: "FK_Streets_Neighboors_NeighboorId",
                table: "Streets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Streets",
                table: "Streets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Neighboors",
                table: "Neighboors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Homes",
                table: "Homes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Buildings",
                table: "Buildings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Addressinfos",
                table: "Addressinfos");

            migrationBuilder.RenameTable(
                name: "Streets",
                newName: "streets");

            migrationBuilder.RenameTable(
                name: "Neighboors",
                newName: "neighboors");

            migrationBuilder.RenameTable(
                name: "Homes",
                newName: "homes");

            migrationBuilder.RenameTable(
                name: "Buildings",
                newName: "buildings");

            migrationBuilder.RenameTable(
                name: "Addressinfos",
                newName: "addressinfos");

            migrationBuilder.RenameIndex(
                name: "IX_Streets_NeighboorId",
                table: "streets",
                newName: "IX_streets_NeighboorId");

            migrationBuilder.RenameIndex(
                name: "IX_Neighboors_TownId",
                table: "neighboors",
                newName: "IX_neighboors_TownId");

            migrationBuilder.RenameIndex(
                name: "IX_Homes_BuildingId",
                table: "homes",
                newName: "IX_homes_BuildingId");

            migrationBuilder.RenameIndex(
                name: "IX_Buildings_StreetId",
                table: "buildings",
                newName: "IX_buildings_StreetId");

            migrationBuilder.RenameIndex(
                name: "IX_Addressinfos_HomeId",
                table: "addressinfos",
                newName: "IX_addressinfos_HomeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_streets",
                table: "streets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_neighboors",
                table: "neighboors",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_homes",
                table: "homes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_buildings",
                table: "buildings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_addressinfos",
                table: "addressinfos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_addressinfos_homes_HomeId",
                table: "addressinfos",
                column: "HomeId",
                principalTable: "homes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_buildings_streets_StreetId",
                table: "buildings",
                column: "StreetId",
                principalTable: "streets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_homes_buildings_BuildingId",
                table: "homes",
                column: "BuildingId",
                principalTable: "buildings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_neighboors_Town_TownId",
                table: "neighboors",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_streets_neighboors_NeighboorId",
                table: "streets",
                column: "NeighboorId",
                principalTable: "neighboors",
                principalColumn: "Id");
        }
    }
}
