using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoTrackingToOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CargoExternalId",
                table: "OrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CargoRequestHandled",
                table: "OrderItems",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoTrackNumber",
                table: "OrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoTrackUrl",
                table: "OrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShipmentDate",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoExternalId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CargoRequestHandled",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CargoTrackNumber",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CargoTrackUrl",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ShipmentDate",
                table: "OrderItems");
        }
    }
}
