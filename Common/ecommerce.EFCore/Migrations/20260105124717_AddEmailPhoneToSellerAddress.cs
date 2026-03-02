using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailPhoneToSellerAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNumer",
                table: "SellerAddresses",
                newName: "PhoneNumber");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "SellerAddresses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "SellerAddresses");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "SellerAddresses",
                newName: "PhoneNumer");
        }
    }
}
