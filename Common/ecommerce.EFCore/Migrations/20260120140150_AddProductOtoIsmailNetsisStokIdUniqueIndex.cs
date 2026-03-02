using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOtoIsmailNetsisStokIdUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FIXED: Önce duplicate NetsisStokId değerlerini temizle
            // En son ModifiedDate'e sahip olanı tut, diğerlerini sil
            migrationBuilder.Sql(@"
                DELETE FROM ""ProductOtoIsmails""
                WHERE ""Id"" IN (
                    SELECT ""Id""
                    FROM (
                        SELECT ""Id"",
                               ROW_NUMBER() OVER (
                                   PARTITION BY ""NetsisStokId"" 
                                   ORDER BY COALESCE(""ModifiedDate"", ""CreatedDate"") DESC
                               ) as rn
                        FROM ""ProductOtoIsmails""
                    ) ranked
                    WHERE rn > 1
                );
            ");

            // Duplicate'ler temizlendi, şimdi unique index oluştur
            migrationBuilder.CreateIndex(
                name: "IX_ProductOtoIsmails_NetsisStokId",
                table: "ProductOtoIsmails",
                column: "NetsisStokId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductOtoIsmails_NetsisStokId",
                table: "ProductOtoIsmails");
        }
    }
}
