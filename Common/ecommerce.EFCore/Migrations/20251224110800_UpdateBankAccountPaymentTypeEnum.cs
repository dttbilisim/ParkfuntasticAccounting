using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBankAccountPaymentTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add temporary integer column
            migrationBuilder.AddColumn<int>(
                name: "PaymentTypeTemp",
                table: "BankAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Map existing string values to enum integers
            migrationBuilder.Sql(@"
                UPDATE ""BankAccounts""
                SET ""PaymentTypeTemp"" = CASE 
                    WHEN ""PaymentType"" = 'Nakit' THEN 1
                    WHEN ""PaymentType"" = 'Kredi Kartı' THEN 2
                    WHEN ""PaymentType"" = 'Havale/EFT' THEN 3
                    WHEN ""PaymentType"" = 'Çek' THEN 4
                    ELSE 1
                END
            ");

            // Drop old column
            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "BankAccounts");

            // Rename temporary column
            migrationBuilder.RenameColumn(
                name: "PaymentTypeTemp",
                table: "BankAccounts",
                newName: "PaymentType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add temporary string column
            migrationBuilder.AddColumn<string>(
                name: "PaymentTypeTemp",
                table: "BankAccounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Nakit");

            // Map enum integers back to string values
            migrationBuilder.Sql(@"
                UPDATE ""BankAccounts""
                SET ""PaymentTypeTemp"" = CASE 
                    WHEN ""PaymentType"" = 1 THEN 'Nakit'
                    WHEN ""PaymentType"" = 2 THEN 'Kredi Kartı'
                    WHEN ""PaymentType"" = 3 THEN 'Havale/EFT'
                    WHEN ""PaymentType"" = 4 THEN 'Çek'
                    ELSE 'Nakit'
                END
            ");

            // Drop old column
            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "BankAccounts");

            // Rename temporary column
            migrationBuilder.RenameColumn(
                name: "PaymentTypeTemp",
                table: "BankAccounts",
                newName: "PaymentType");
        }
    }
}
