using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMainGroupIdToProductForPcPos : Migration
    {
        /// <inheritdoc />
        /// <summary>PcPos: TransferProductsAsync p.MainGroupId kullanıyor. MainGroupId = CategoryId (generated column)</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Product"" 
                ADD COLUMN ""MainGroupId"" integer GENERATED ALWAYS AS (""CategoryId"") STORED;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainGroupId",
                table: "Product");
        }
    }
}
