using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class BackfillOrdersCustomerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. ApplicationUser.CustomerId üzerinden (siparişi oluşturan kullanıcının carisi)
            migrationBuilder.Sql(@"
                UPDATE ""Orders"" o
                SET ""CustomerId"" = au.""CustomerId""
                FROM ""AspNetUsers"" au
                WHERE o.""CompanyId"" = au.""Id""
                  AND au.""CustomerId"" IS NOT NULL
                  AND o.""CustomerId"" IS NULL
                  AND o.""Status"" != 99
            ");

            // 2. CustomerAccountTransactions üzerinden (siparişe bağlı cari hareketi varsa)
            migrationBuilder.Sql(@"
                UPDATE ""Orders"" o
                SET ""CustomerId"" = sub.""CustomerId""
                FROM (
                  SELECT DISTINCT ON (cat.""OrderId"") cat.""OrderId"", cat.""CustomerId""
                  FROM ""CustomerAccountTransactions"" cat
                  WHERE cat.""OrderId"" IS NOT NULL
                    AND cat.""CustomerId"" IS NOT NULL
                ) sub
                WHERE o.""Id"" = sub.""OrderId""
                  AND o.""CustomerId"" IS NULL
                  AND o.""Status"" != 99
            ");

            // 3. UserAddress -> ApplicationUser.CustomerId (teslimat adresindeki kullanıcının carisi)
            migrationBuilder.Sql(@"
                UPDATE ""Orders"" o
                SET ""CustomerId"" = au.""CustomerId""
                FROM ""UserAddresses"" ua
                JOIN ""AspNetUsers"" au ON ua.""ApplicationUserId"" = au.""Id""
                WHERE o.""UserAddressId"" = ua.""Id""
                  AND au.""CustomerId"" IS NOT NULL
                  AND o.""CustomerId"" IS NULL
                  AND o.""Status"" != 99
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
