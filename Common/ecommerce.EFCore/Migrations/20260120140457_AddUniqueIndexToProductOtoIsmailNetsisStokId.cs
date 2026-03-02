using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToProductOtoIsmailNetsisStokId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bu migration boş - duplicate temizleme ve index oluşturma
            // 20260120140150_AddProductOtoIsmailNetsisStokIdUniqueIndex migration'ında yapıldı
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
