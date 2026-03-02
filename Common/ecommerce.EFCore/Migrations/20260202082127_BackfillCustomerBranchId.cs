using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCustomerBranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill BranchId from CustomerBranches (taking the first one)
            // This ensures existing customers become visible under the new filtering logic
            migrationBuilder.Sql(@"
                UPDATE ""Customers""
                SET ""BranchId"" = (
                    SELECT ""BranchId"" 
                    FROM ""CustomerBranches"" 
                    WHERE ""CustomerBranches"".""CustomerId"" = ""Customers"".""Id"" 
                    LIMIT 1
                )
                WHERE ""BranchId"" IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
