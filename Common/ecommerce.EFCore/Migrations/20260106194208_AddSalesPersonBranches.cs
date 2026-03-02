using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPersonBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_Branches_BranchId",
                table: "SalesPersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_Corporations_CorporationId",
                table: "SalesPersons");

            migrationBuilder.AlterColumn<int>(
                name: "CorporationId",
                table: "SalesPersons",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "SalesPersons",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "SalesPersonBranches",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesPersonId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPersonBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesPersonBranches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesPersonBranches_SalesPersons_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "SalesPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesPersonBranches_BranchId",
                table: "SalesPersonBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesPersonBranches_SalesPersonId",
                table: "SalesPersonBranches",
                column: "SalesPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_Branches_BranchId",
                table: "SalesPersons",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_Corporations_CorporationId",
                table: "SalesPersons",
                column: "CorporationId",
                principalTable: "Corporations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_Branches_BranchId",
                table: "SalesPersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesPersons_Corporations_CorporationId",
                table: "SalesPersons");

            migrationBuilder.DropTable(
                name: "SalesPersonBranches");

            migrationBuilder.AlterColumn<int>(
                name: "CorporationId",
                table: "SalesPersons",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "SalesPersons",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_Branches_BranchId",
                table: "SalesPersons",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesPersons_Corporations_CorporationId",
                table: "SalesPersons",
                column: "CorporationId",
                principalTable: "Corporations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
