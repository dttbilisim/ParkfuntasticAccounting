using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "ProductStocks",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "StockTransferLogs",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SourceWarehouseId = table.Column<int>(type: "integer", nullable: false),
                    TargetWarehouseId = table.Column<int>(type: "integer", nullable: false),
                    SourceShelfId = table.Column<int>(type: "integer", nullable: false),
                    TargetShelfId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    TransferredByUserId = table.Column<int>(type: "integer", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferLogs_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferLogs_WarehouseShelves_SourceShelfId",
                        column: x => x.SourceShelfId,
                        principalTable: "WarehouseShelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferLogs_WarehouseShelves_TargetShelfId",
                        column: x => x.TargetShelfId,
                        principalTable: "WarehouseShelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferLogs_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferLogs_Warehouses_TargetWarehouseId",
                        column: x => x.TargetWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLogs_ProductId",
                table: "StockTransferLogs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLogs_SourceShelfId",
                table: "StockTransferLogs",
                column: "SourceShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLogs_SourceWarehouseId",
                table: "StockTransferLogs",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLogs_TargetShelfId",
                table: "StockTransferLogs",
                column: "TargetShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLogs_TargetWarehouseId",
                table: "StockTransferLogs",
                column: "TargetWarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransferLogs");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "ProductStocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
