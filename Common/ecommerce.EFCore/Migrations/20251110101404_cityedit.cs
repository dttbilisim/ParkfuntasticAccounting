using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class cityedit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "CityCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "HomeCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "HomeId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "NeighboorCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "NeighboorId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "StreetCode",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "StreetId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "TownCode",
                table: "UserAddresses");

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Sellers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TownId",
                table: "Sellers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SystemName = table.Column<string>(type: "text", nullable: false),
                    BankCode = table.Column<int>(type: "integer", nullable: false),
                    LogoPath = table.Column<string>(type: "text", nullable: false),
                    UseCommonPaymentPage = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultBank = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    ManufacturerCard = table.Column<bool>(type: "boolean", nullable: false),
                    CampaignCard = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankCards_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankParameters_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankPaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionNumber = table.Column<string>(type: "text", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    UserIpAddress = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    BankId = table.Column<int>(type: "integer", nullable: false),
                    CardPrefix = table.Column<string>(type: "text", nullable: false),
                    CardHolderName = table.Column<string>(type: "text", nullable: false),
                    Installment = table.Column<int>(type: "integer", nullable: false),
                    ExtraInstallment = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    BankErrorMessage = table.Column<string>(type: "text", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    BankRequest = table.Column<string>(type: "text", nullable: false),
                    BankResponse = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankPaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankPaymentTransactions_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankCreditCardInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreditCardId = table.Column<int>(type: "integer", nullable: false),
                    Installment = table.Column<int>(type: "integer", nullable: false),
                    InstallmentRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BankId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCreditCardInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankCreditCardInstallments_BankCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "BankCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankCreditCardInstallments_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BankCreditCardPrefixs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreditCardId = table.Column<int>(type: "integer", nullable: false),
                    Prefix = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCreditCardPrefixs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankCreditCardPrefixs_BankCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalTable: "BankCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_UserAddresses_CityId\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_UserAddresses_TownId\";");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_CityId",
                table: "UserAddresses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_TownId",
                table: "UserAddresses",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_CityId",
                table: "Sellers",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_TownId",
                table: "Sellers",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_BankCards_BankId",
                table: "BankCards",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BankCreditCardInstallments_BankId",
                table: "BankCreditCardInstallments",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BankCreditCardInstallments_CreditCardId",
                table: "BankCreditCardInstallments",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_BankCreditCardPrefixs_CreditCardId",
                table: "BankCreditCardPrefixs",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_BankParameters_BankId",
                table: "BankParameters",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BankPaymentTransactions_BankId",
                table: "BankPaymentTransactions",
                column: "BankId");

            migrationBuilder.Sql("ALTER TABLE \"UserAddresses\" DROP CONSTRAINT IF EXISTS \"FK_UserAddresses_City_CityId\";");
            migrationBuilder.Sql("ALTER TABLE \"UserAddresses\" DROP CONSTRAINT IF EXISTS \"FK_UserAddresses_Town_TownId\";");

            migrationBuilder.AddForeignKey(
                name: "FK_Sellers_City_CityId",
                table: "Sellers",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sellers_Town_TownId",
                table: "Sellers",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_City_CityId",
                table: "UserAddresses",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_Town_TownId",
                table: "UserAddresses",
                column: "TownId",
                principalTable: "Town",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sellers_City_CityId",
                table: "Sellers");

            migrationBuilder.DropForeignKey(
                name: "FK_Sellers_Town_TownId",
                table: "Sellers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_City_CityId",
                table: "UserAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_Town_TownId",
                table: "UserAddresses");

            migrationBuilder.DropTable(
                name: "BankCreditCardInstallments");

            migrationBuilder.DropTable(
                name: "BankCreditCardPrefixs");

            migrationBuilder.DropTable(
                name: "BankParameters");

            migrationBuilder.DropTable(
                name: "BankPaymentTransactions");

            migrationBuilder.DropTable(
                name: "BankCards");

            migrationBuilder.DropTable(
                name: "Banks");

            migrationBuilder.DropIndex(
                name: "IX_UserAddresses_CityId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_UserAddresses_TownId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_Sellers_CityId",
                table: "Sellers");

            migrationBuilder.DropIndex(
                name: "IX_Sellers_TownId",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "TownId",
                table: "Sellers");

            migrationBuilder.AddColumn<string>(
                name: "BuildingCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NeighboorCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeighboorId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreetId",
                table: "UserAddresses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TownCode",
                table: "UserAddresses",
                type: "text",
                nullable: true);
        }
    }
}
