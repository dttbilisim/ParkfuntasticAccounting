using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrdersCompanyIdFKConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove FK_Orders_AspNetUsers_CompanyId constraint to allow CompanyId to reference either Users or AspNetUsers
            // Web context uses Users table, Admin/B2B context uses AspNetUsers table
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_CompanyId",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore FK_Orders_AspNetUsers_CompanyId constraint if needed
            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_CompanyId",
                table: "Orders",
                column: "CompanyId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
