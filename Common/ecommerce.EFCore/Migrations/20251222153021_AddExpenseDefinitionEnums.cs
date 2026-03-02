using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseDefinitionEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Geçici bir integer kolonu oluştur
            migrationBuilder.AddColumn<int>(
                name: "OperationTypeTemp",
                table: "ExpenseDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // String değerleri enum değerlerine map edip geçici kolona kopyala
            // "Gider İşlemleri" veya "Gider" içeren değerleri 1'e (Gider), diğerlerini 2'ye (Gelir) map et
            migrationBuilder.Sql(@"
                UPDATE ""ExpenseDefinitions""
                SET ""OperationTypeTemp"" = CASE 
                    WHEN ""OperationType"" ILIKE '%Gider%' THEN 1
                    WHEN ""OperationType"" ILIKE '%Gelir%' THEN 2
                    ELSE 1
                END;
            ");

            // Eski kolonu sil
            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "ExpenseDefinitions");

            // Geçici kolonu asıl kolon olarak yeniden adlandır
            migrationBuilder.RenameColumn(
                name: "OperationTypeTemp",
                table: "ExpenseDefinitions",
                newName: "OperationType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geçici bir string kolonu oluştur
            migrationBuilder.AddColumn<string>(
                name: "OperationTypeTemp",
                table: "ExpenseDefinitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Gider İşlemleri");

            // Integer değerleri string'e cast edip geçici kolona kopyala
            migrationBuilder.Sql(@"
                UPDATE ""ExpenseDefinitions""
                SET ""OperationTypeTemp"" = CASE 
                    WHEN ""OperationType"" = 1 THEN 'Gider İşlemleri'
                    WHEN ""OperationType"" = 2 THEN 'Gelir İşlemleri'
                    ELSE 'Gider İşlemleri'
                END;
            ");

            // Eski kolonu sil
            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "ExpenseDefinitions");

            // Geçici kolonu asıl kolon olarak yeniden adlandır
            migrationBuilder.RenameColumn(
                name: "OperationTypeTemp",
                table: "ExpenseDefinitions",
                newName: "OperationType");
        }
    }
}
