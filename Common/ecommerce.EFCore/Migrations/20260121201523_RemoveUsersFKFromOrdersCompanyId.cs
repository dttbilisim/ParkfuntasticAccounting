using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUsersFKFromOrdersCompanyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove FK_Orders_Users_CompanyId constraint - B2B uses ApplicationUser, not User
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_CompanyId",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore FK_Orders_Users_CompanyId constraint if needed
            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_CompanyId",
                table: "Orders",
                column: "CompanyId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
