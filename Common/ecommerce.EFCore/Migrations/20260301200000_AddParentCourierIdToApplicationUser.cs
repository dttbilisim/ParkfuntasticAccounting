using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <summary>
    /// Alt kurye kullanıcıları üst kuryeye bağlamak için AspNetUsers tablosuna ParentCourierId (üst kurye UserId) ekler.
    /// </summary>
    public partial class AddParentCourierIdToApplicationUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCourierId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ParentCourierId",
                table: "AspNetUsers",
                column: "ParentCourierId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ParentCourierId",
                table: "AspNetUsers",
                column: "ParentCourierId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ParentCourierId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ParentCourierId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ParentCourierId",
                table: "AspNetUsers");
        }
    }
}
