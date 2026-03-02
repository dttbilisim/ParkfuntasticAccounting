using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId",
                table: "CompanyCargoes");

            migrationBuilder.DropIndex(
                name: "IX_CompanyCargoes_CompanyId",
                table: "CompanyCargoes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "CompanyCargoes");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Brand",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Brand");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "CompanyCargoes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCargoes_CompanyId",
                table: "CompanyCargoes",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId",
                table: "CompanyCargoes",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id");
        }
    }
}
