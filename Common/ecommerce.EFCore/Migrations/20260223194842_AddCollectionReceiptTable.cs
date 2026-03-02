using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionReceiptTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionReceipts",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MakbuzNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SalesPersonId = table.Column<int>(type: "integer", nullable: false),
                    CustomerAccountTransactionId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionReceipts_CustomerAccountTransactions_CustomerAcco~",
                        column: x => x.CustomerAccountTransactionId,
                        principalTable: "CustomerAccountTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionReceipts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionReceipts_SalesPersons_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "SalesPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionReceipts_CustomerAccountTransactionId",
                table: "CollectionReceipts",
                column: "CustomerAccountTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionReceipts_CustomerId",
                table: "CollectionReceipts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionReceipts_MakbuzNo",
                table: "CollectionReceipts",
                column: "MakbuzNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionReceipts_SalesPersonId",
                table: "CollectionReceipts",
                column: "SalesPersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionReceipts");
        }
    }
}
