using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingOrdersPlatformType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing B2B orders: If CompanyId exists in AspNetUsers, set PlatformType to B2B (2)
            // Otherwise, keep as Marketplace (1) - default value
            migrationBuilder.Sql(@"
                UPDATE ""Orders""
                SET ""PlatformType"" = 2
                WHERE ""CompanyId"" IN (SELECT ""Id"" FROM ""AspNetUsers"")
                  AND ""PlatformType"" = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: Set all B2B orders back to Marketplace (1)
            migrationBuilder.Sql(@"
                UPDATE ""Orders""
                SET ""PlatformType"" = 1
                WHERE ""PlatformType"" = 2;
            ");
        }
    }
}
