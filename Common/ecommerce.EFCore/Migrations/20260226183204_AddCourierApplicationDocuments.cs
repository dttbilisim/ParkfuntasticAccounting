using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierApplicationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IBAN",
                table: "CourierApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCopyPath",
                table: "CourierApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureDeclarationPath",
                table: "CourierApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "CourierApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxOffice",
                table: "CourierApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxPlatePath",
                table: "CourierApplications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IBAN",
                table: "CourierApplications");

            migrationBuilder.DropColumn(
                name: "IdCopyPath",
                table: "CourierApplications");

            migrationBuilder.DropColumn(
                name: "SignatureDeclarationPath",
                table: "CourierApplications");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "CourierApplications");

            migrationBuilder.DropColumn(
                name: "TaxOffice",
                table: "CourierApplications");

            migrationBuilder.DropColumn(
                name: "TaxPlatePath",
                table: "CourierApplications");
        }
    }
}
