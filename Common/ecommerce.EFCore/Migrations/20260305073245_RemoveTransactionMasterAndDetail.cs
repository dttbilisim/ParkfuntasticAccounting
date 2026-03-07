using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTransactionMasterAndDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tTransaction");

            migrationBuilder.DropTable(
                name: "TransactionMaster");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionMaster",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CaseID = table.Column<int>(type: "integer", nullable: true),
                    CaseNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CashHandle = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompanyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrencyId = table.Column<int>(type: "integer", nullable: true),
                    CurrencyType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CustomerID = table.Column<int>(type: "integer", nullable: true),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountTotalCurrency = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    EFaturaNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Ettn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDay = table.Column<int>(type: "integer", nullable: false),
                    GrossTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    InvoiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InvoiceStatus = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsClose = table.Column<bool>(type: "boolean", nullable: false),
                    IsDispatch = table.Column<bool>(type: "boolean", nullable: false),
                    IsEArsiv = table.Column<bool>(type: "boolean", nullable: false),
                    IsEFatura = table.Column<bool>(type: "boolean", nullable: false),
                    IsSendInvoice = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: true),
                    PaymentHire = table.Column<int>(type: "integer", nullable: false),
                    PaymentType = table.Column<int>(type: "integer", nullable: true),
                    PlasiyerID = table.Column<int>(type: "integer", nullable: true),
                    ProcessType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Response = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SerialCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubDiscountTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SubTotalCurrency = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCurrency = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    TransCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TransDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VatStatus = table.Column<bool>(type: "boolean", nullable: false),
                    VatTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    VatTotalCurrency = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tTransaction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterID = table.Column<long>(type: "bigint", nullable: false),
                    BranchID = table.Column<int>(type: "integer", nullable: true),
                    CDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CellID = table.Column<int>(type: "integer", nullable: false),
                    CompanyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrencyId = table.Column<int>(type: "integer", nullable: true),
                    CustomerID = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Discount1 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount2 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount3 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount4 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount5 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    FatID = table.Column<long>(type: "bigint", nullable: true),
                    InvoiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InvoiceStatus = table.Column<bool>(type: "boolean", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProcessType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProfitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SerialCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<bool>(type: "boolean", nullable: false),
                    StockID = table.Column<int>(type: "integer", nullable: true),
                    StockName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StockType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StockUnityType = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TransCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VatCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tTransaction_TransactionMaster_MasterID",
                        column: x => x.MasterID,
                        principalTable: "TransactionMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_BranchId",
                table: "CashRegisters",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionMaster_CompanyCode_Year_Month_TransCode",
                table: "TransactionMaster",
                columns: new[] { "CompanyCode", "Year", "Month", "TransCode" });

            migrationBuilder.CreateIndex(
                name: "IX_tTransaction_MasterID",
                table: "tTransaction",
                column: "MasterID");
        }
    }
}
