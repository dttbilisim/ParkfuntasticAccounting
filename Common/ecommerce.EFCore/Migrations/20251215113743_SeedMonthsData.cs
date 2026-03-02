using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedMonthsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Months",
                columns: new[] { "Id", "Name", "MonthNumber", "Order" },
                values: new object[,]
                {
                    { 1, "Ocak", 1, 1 },
                    { 2, "Şubat", 2, 2 },
                    { 3, "Mart", 3, 3 },
                    { 4, "Nisan", 4, 4 },
                    { 5, "Mayıs", 5, 5 },
                    { 6, "Haziran", 6, 6 },
                    { 7, "Temmuz", 7, 7 },
                    { 8, "Ağustos", 8, 8 },
                    { 9, "Eylül", 9, 9 },
                    { 10, "Ekim", 10, 10 },
                    { 11, "Kasım", 11, 11 },
                    { 12, "Aralık", 12, 12 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Months",
                keyColumn: "Id",
                keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
        }
    }
}
