using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceTypeId = table.Column<int>(type: "integer", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InvoiceSerialNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Warehouse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WarehouseId = table.Column<int>(type: "integer", nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentTypeId = table.Column<int>(type: "integer", nullable: true),
                    CashRegisterId = table.Column<int>(type: "integer", nullable: true),
                    SalesPersonId = table.Column<int>(type: "integer", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsVatIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEInvoice = table.Column<bool>(type: "boolean", nullable: false),
                    IsEArchive = table.Column<bool>(type: "boolean", nullable: false),
                    IsCashSale = table.Column<bool>(type: "boolean", nullable: false),
                    UseProjectionCalculation = table.Column<bool>(type: "boolean", nullable: false),
                    CalculateFromListPrice = table.Column<bool>(type: "boolean", nullable: false),
                    UseCustomerLastInvoiceAddress = table.Column<bool>(type: "boolean", nullable: false),
                    RiskLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    RiskLimitText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CurrentBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    LastServiceTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageMaturity = table.Column<int>(type: "integer", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount1 = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount2 = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount3 = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount4 = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount5 = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmountCurrency = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountTotalCurrency = table.Column<decimal>(type: "numeric", nullable: false),
                    VatTotalCurrency = table.Column<decimal>(type: "numeric", nullable: false),
                    GeneralTotalCurrency = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    VatTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    GeneralTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invoices_InvoiceTypes_InvoiceTypeId",
                        column: x => x.InvoiceTypeId,
                        principalTable: "InvoiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invoices_SalesPersons_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "SalesPersons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount1 = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount2 = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount3 = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount4 = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount5 = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ProductId",
                table: "InvoiceItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CurrencyId",
                table: "Invoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceTypeId",
                table: "Invoices",
                column: "InvoiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SalesPersonId",
                table: "Invoices",
                column: "SalesPersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
