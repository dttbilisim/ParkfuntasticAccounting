using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class DropLowercaseTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop lowercase tables if they still exist
            migrationBuilder.Sql("DROP TABLE IF EXISTS city CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS town CASCADE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
