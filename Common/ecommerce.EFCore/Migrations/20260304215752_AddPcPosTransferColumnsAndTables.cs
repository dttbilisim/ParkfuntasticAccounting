using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPcPosTransferColumnsAndTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "ProductUnits",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusPcPos",
                table: "Product",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusSales",
                table: "Product",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "PaymentTypes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "PaymentTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPcPos",
                table: "PaymentTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "PaymentTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCredit",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentPricesUpdatable",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPcPos",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStreetAgency",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatExcluded",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CaseIds",
                table: "AspNetUsers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdit",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBranches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBranches_BranchId",
                table: "ProductBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBranches_ProductId_BranchId",
                table: "ProductBranches",
                columns: new[] { "ProductId", "BranchId" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "ProductSaleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    CurrencyId = table.Column<int>(type: "integer", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSaleItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaleOptions",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleOptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSaleItems_CurrencyId",
                table: "ProductSaleItems",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSaleItems_ProductId",
                table: "ProductSaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSaleItems_RefProductId",
                table: "ProductSaleItems",
                column: "RefProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTypes_CurrencyId",
                table: "PaymentTypes",
                column: "CurrencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductBranches");

            migrationBuilder.DropTable(
                name: "ProductSaleItems");

            migrationBuilder.DropTable(
                name: "SaleOptions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTypes_CurrencyId",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "ProductUnits");

            migrationBuilder.DropColumn(
                name: "StatusPcPos",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "StatusSales",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "IsPcPos",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PaymentTypes");

            migrationBuilder.DropColumn(
                name: "IsCredit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsCurrentPricesUpdatable",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsPcPos",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsStreetAgency",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsVatExcluded",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CaseIds",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsEdit",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "AspNetUsers");
        }
    }
}
