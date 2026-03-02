using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerWorkingTypeToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "CustomerWorkingType",
                table: "Customers",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1); // Default: Pesin (1)

            // Update existing records to Pesin (1) as default
            migrationBuilder.Sql(@"
                UPDATE ""Customers""
                SET ""CustomerWorkingType"" = 1
                WHERE ""CustomerWorkingType"" = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerWorkingType",
                table: "Customers");
        }
    }
}
