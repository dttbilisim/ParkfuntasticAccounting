using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateOrderMenuToB2BMyOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sipariş alt menülerini b2b/my-orders'a yönlendir
            migrationBuilder.Sql(@"
                UPDATE ""Menus"" SET ""Path"" = 'b2b/my-orders?tab=onay-bekleyen' WHERE ""Path"" = 'orders/pending';
                UPDATE ""Menus"" SET ""Path"" = 'b2b/my-orders' WHERE ""Path"" = 'orders/approved';
                UPDATE ""Menus"" SET ""Path"" = 'b2b/my-orders' WHERE ""Path"" = 'orders/all';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Menus"" SET ""Path"" = 'orders/pending' WHERE ""Path"" = 'b2b/my-orders?tab=onay-bekleyen';
                UPDATE ""Menus"" SET ""Path"" = 'orders/approved' WHERE ""Path"" = 'b2b/my-orders' AND ""Name"" = 'Onaylı Siparişler';
                UPDATE ""Menus"" SET ""Path"" = 'orders/all' WHERE ""Path"" = 'b2b/my-orders' AND ""Name"" = 'Tüm Siparişler';
            ");
        }
    }
}
