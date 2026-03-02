using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "CourierDeliveryStatus",
                table: "Orders",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourierId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DeliveryOptionType",
                table: "Orders",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedCourierDeliveryMinutes",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourierApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    IdentityNumber = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierApplications_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Couriers",
                columns: table => new
                {
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedId = table.Column<int>(type: "integer", nullable: false),
                    ModifiedId = table.Column<int>(type: "integer", nullable: true),
                    DeletedId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Couriers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Couriers_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourierLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourierId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Accuracy = table.Column<double>(type: "double precision", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierLocations_Couriers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "Couriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourierLocations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourierServiceAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourierId = table.Column<int>(type: "integer", nullable: false),
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    TownId = table.Column<int>(type: "integer", nullable: false),
                    NeighboorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierServiceAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierServiceAreas_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourierServiceAreas_Couriers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "Couriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourierServiceAreas_Neighboors_NeighboorId",
                        column: x => x.NeighboorId,
                        principalTable: "Neighboors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourierServiceAreas_Town_TownId",
                        column: x => x.TownId,
                        principalTable: "Town",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CourierId",
                table: "Orders",
                column: "CourierId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierApplications_ApplicationUserId",
                table: "CourierApplications",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierLocations_CourierId",
                table: "CourierLocations",
                column: "CourierId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierLocations_OrderId",
                table: "CourierLocations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Couriers_ApplicationUserId",
                table: "Couriers",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CityId",
                table: "CourierServiceAreas",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierId_CityId_TownId",
                table: "CourierServiceAreas",
                columns: new[] { "CourierId", "CityId", "TownId" },
                unique: true,
                filter: "\"NeighboorId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierId_CityId_TownId_NeighboorId",
                table: "CourierServiceAreas",
                columns: new[] { "CourierId", "CityId", "TownId", "NeighboorId" },
                unique: true,
                filter: "\"NeighboorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_NeighboorId",
                table: "CourierServiceAreas",
                column: "NeighboorId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_TownId",
                table: "CourierServiceAreas",
                column: "TownId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Couriers_CourierId",
                table: "Orders",
                column: "CourierId",
                principalTable: "Couriers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Couriers_CourierId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "CourierApplications");

            migrationBuilder.DropTable(
                name: "CourierLocations");

            migrationBuilder.DropTable(
                name: "CourierServiceAreas");

            migrationBuilder.DropTable(
                name: "Couriers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CourierId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CourierDeliveryStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CourierId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryOptionType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EstimatedCourierDeliveryMinutes",
                table: "Orders");
        }
    }
}
