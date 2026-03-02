using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class cargoedit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId",
                table: "CompanyCargoes");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "CompanyCargoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCargoes_CompanyId1",
                table: "CompanyCargoes",
                column: "CompanyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId1",
                table: "CompanyCargoes",
                column: "CompanyId1",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyCargoes_Sellers_CompanyId",
                table: "CompanyCargoes",
                column: "CompanyId",
                principalTable: "Sellers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId1",
                table: "CompanyCargoes");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCargoes_Sellers_CompanyId",
                table: "CompanyCargoes");

            migrationBuilder.DropIndex(
                name: "IX_CompanyCargoes_CompanyId1",
                table: "CompanyCargoes");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "CompanyCargoes");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId",
                table: "CompanyCargoes",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
