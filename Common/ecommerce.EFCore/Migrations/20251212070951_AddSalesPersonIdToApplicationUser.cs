using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPersonIdToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalesPersonId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SalesPersonId",
                table: "AspNetUsers",
                column: "SalesPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_SalesPersons_SalesPersonId",
                table: "AspNetUsers",
                column: "SalesPersonId",
                principalTable: "SalesPersons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_SalesPersons_SalesPersonId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SalesPersonId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SalesPersonId",
                table: "AspNetUsers");
        }
    }
}
