using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTownIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TownId",
                table: "Neighboors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Neighboors_TownId",
                table: "Neighboors",
                column: "TownId");

            migrationBuilder.AddForeignKey(
                name: "FK_Neighboors_Town_TownId",
                table: "Neighboors",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Neighboors_Town_TownId",
                table: "Neighboors");

            migrationBuilder.DropIndex(
                name: "IX_Neighboors_TownId",
                table: "Neighboors");

            migrationBuilder.DropColumn(
                name: "TownId",
                table: "Neighboors");
        }
    }
}
