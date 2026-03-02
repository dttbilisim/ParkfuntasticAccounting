using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class FixBrandIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Brand\" DROP CONSTRAINT IF EXISTS \"Name\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"Name\";");

            /* Indexes already exist in DB
            migrationBuilder.CreateIndex(
                name: "IX_Tiers_BranchId_Name",
                table: "Tiers",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"Status\" <> 99");

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_BranchId_Title",
                table: "Surveys",
                columns: new[] { "BranchId", "Title" },
                unique: true,
                filter: "\"Status\" <> 99");

            migrationBuilder.CreateIndex(
                name: "IX_ProductType_BranchId_Name",
                table: "ProductType",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"Status\" <> 99");

            migrationBuilder.CreateIndex(
                name: "IX_Category_BranchId_Name",
                table: "Category",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"Status\" <> 99");
            */

            /* 
            migrationBuilder.CreateIndex(
                name: "IX_Brand_BranchId_Name",
                table: "Brand",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"Status\" <> 99");
             */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tiers_BranchId_Name",
                table: "Tiers");

            migrationBuilder.DropIndex(
                name: "IX_Surveys_BranchId_Title",
                table: "Surveys");

            migrationBuilder.DropIndex(
                name: "IX_ProductType_BranchId_Name",
                table: "ProductType");

            migrationBuilder.DropIndex(
                name: "IX_Category_BranchId_Name",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "IX_Brand_BranchId_Name",
                table: "Brand");
        }
    }
}
