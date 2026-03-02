using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkOperationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductRemars_Code",
                table: "ProductRemars",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductDegas_Code",
                table: "ProductDegas",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBasbugs_No",
                table: "ProductBasbugs",
                column: "No",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductRemars_Code",
                table: "ProductRemars");

            migrationBuilder.DropIndex(
                name: "IX_ProductDegas_Code",
                table: "ProductDegas");

            migrationBuilder.DropIndex(
                name: "IX_ProductBasbugs_No",
                table: "ProductBasbugs");
        }
    }
}
