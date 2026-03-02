using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class cargoedit1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId1",
                table: "CompanyCargoes");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCargoes_Sellers_CompanyId",
                table: "CompanyCargoes");

            migrationBuilder.RenameColumn(
                name: "CompanyId1",
                table: "CompanyCargoes",
                newName: "SellerId");

            migrationBuilder.RenameIndex(
                name: "IX_CompanyCargoes_CompanyId1",
                table: "CompanyCargoes",
                newName: "IX_CompanyCargoes_SellerId");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "CompanyCargoes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId",
                table: "CompanyCargoes",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyCargoes_Sellers_SellerId",
                table: "CompanyCargoes",
                column: "SellerId",
                principalTable: "Sellers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCargoes_Company_CompanyId",
                table: "CompanyCargoes");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyCargoes_Sellers_SellerId",
                table: "CompanyCargoes");

            migrationBuilder.RenameColumn(
                name: "SellerId",
                table: "CompanyCargoes",
                newName: "CompanyId1");

            migrationBuilder.RenameIndex(
                name: "IX_CompanyCargoes_SellerId",
                table: "CompanyCargoes",
                newName: "IX_CompanyCargoes_CompanyId1");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "CompanyCargoes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

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
    }
}
