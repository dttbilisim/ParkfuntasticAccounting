using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSubmenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add order submenu items - parent "Siparişler" (Path 'orders') may exist
            migrationBuilder.Sql(@"
                INSERT INTO ""Menus"" (""Name"", ""Path"", ""Icon"", ""Order"", ""ParentId"")
                SELECT 'Onay Bekleyen', 'orders/pending', 'pending_actions', 1, (SELECT ""Id"" FROM ""Menus"" WHERE ""Path"" = 'orders' LIMIT 1)
                WHERE NOT EXISTS (SELECT 1 FROM ""Menus"" WHERE ""Path"" = 'orders/pending');
            ");
            migrationBuilder.Sql(@"
                INSERT INTO ""Menus"" (""Name"", ""Path"", ""Icon"", ""Order"", ""ParentId"")
                SELECT 'Onaylı Siparişler', 'orders/approved', 'check_circle', 2, (SELECT ""Id"" FROM ""Menus"" WHERE ""Path"" = 'orders' LIMIT 1)
                WHERE NOT EXISTS (SELECT 1 FROM ""Menus"" WHERE ""Path"" = 'orders/approved');
            ");
            migrationBuilder.Sql(@"
                INSERT INTO ""Menus"" (""Name"", ""Path"", ""Icon"", ""Order"", ""ParentId"")
                SELECT 'Tüm Siparişler', 'orders/all', 'list', 3, (SELECT ""Id"" FROM ""Menus"" WHERE ""Path"" = 'orders' LIMIT 1)
                WHERE NOT EXISTS (SELECT 1 FROM ""Menus"" WHERE ""Path"" = 'orders/all');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Menus"" WHERE ""Path"" IN ('orders/pending', 'orders/approved', 'orders/all');
            ");
        }
    }
}
