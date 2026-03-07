using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPcPosAndTransactionMasterTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Product.CategoryId
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Product",
                type: "integer",
                nullable: true);
            migrationBuilder.CreateIndex(name: "IX_Product_CategoryId", table: "Product", column: "CategoryId");

            // AspNetUsers.IsActive
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Customers.CompanyCode
            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "Customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // VersionApp tablosu
            migrationBuilder.CreateTable(
                name: "VersionApp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PcPosVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_VersionApp", x => x.Id));
            migrationBuilder.Sql(@"INSERT INTO ""VersionApp"" (""PcPosVersion"") VALUES ('1.0.0');");

            // TransactionMaster - Ana Kayıt Defteri
            migrationBuilder.CreateTable(
                name: "TransactionMaster",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TransDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderID = table.Column<long>(type: "bigint", nullable: true),
                    ProcessType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TransCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SerialCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerID = table.Column<int>(type: "integer", nullable: true),
                    CashHandle = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentType = table.Column<int>(type: "integer", nullable: true),
                    PaymentHire = table.Column<int>(type: "integer", nullable: false),
                    CaseID = table.Column<int>(type: "integer", nullable: true),
                    VatStatus = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiryDay = table.Column<int>(type: "integer", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    VatTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SubDiscountTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    GrossTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    InvoiceStatus = table.Column<bool>(type: "boolean", nullable: false),
                    InvoiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsDispatch = table.Column<bool>(type: "boolean", nullable: false),
                    IsClose = table.Column<bool>(type: "boolean", nullable: false),
                    PlasiyerID = table.Column<int>(type: "integer", nullable: true),
                    IsEFatura = table.Column<bool>(type: "boolean", nullable: false),
                    IsEArsiv = table.Column<bool>(type: "boolean", nullable: false),
                    CUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ettn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Response = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsSendInvoice = table.Column<bool>(type: "boolean", nullable: false),
                    EFaturaNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubTotalCurrency = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    DiscountTotalCurrency = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    VatTotalCurrency = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    TotalCurrency = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    CurrencyType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CaseNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrencyId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_TransactionMaster", x => x.Id));
            migrationBuilder.CreateIndex(name: "IX_TransactionMaster_TransCode", table: "TransactionMaster", column: "TransCode");
            migrationBuilder.CreateIndex(name: "IX_TransactionMaster_CompanyCode_Year_Month_TransCode", table: "TransactionMaster", columns: new[] { "CompanyCode", "Year", "Month", "TransCode" });

            // tTransaction - TransactionMaster kalemleri
            migrationBuilder.CreateTable(
                name: "tTransaction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterID = table.Column<long>(type: "bigint", nullable: false),
                    CompanyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    BranchID = table.Column<int>(type: "integer", nullable: true),
                    CellID = table.Column<int>(type: "integer", nullable: false),
                    StockType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TransCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SerialCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerID = table.Column<int>(type: "integer", nullable: true),
                    StockID = table.Column<int>(type: "integer", nullable: true),
                    StockName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StockUnityType = table.Column<int>(type: "integer", nullable: false),
                    VatCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ProfitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount1 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount2 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount3 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount4 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Discount5 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<bool>(type: "boolean", nullable: false),
                    InvoiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InvoiceStatus = table.Column<bool>(type: "boolean", nullable: false),
                    FatID = table.Column<long>(type: "bigint", nullable: true),
                    CUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CurrencyId = table.Column<int>(type: "integer", nullable: true)
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
            migrationBuilder.CreateIndex(name: "IX_tTransaction_MasterID", table: "tTransaction", column: "MasterID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "tTransaction");
            migrationBuilder.DropTable(name: "TransactionMaster");
            migrationBuilder.DropTable(name: "VersionApp");
            migrationBuilder.DropColumn(name: "CompanyCode", table: "Customers");
            migrationBuilder.DropColumn(name: "IsActive", table: "AspNetUsers");
            migrationBuilder.DropIndex(name: "IX_Product_CategoryId", table: "Product");
            migrationBuilder.DropColumn(name: "CategoryId", table: "Product");
        }
    }
}
