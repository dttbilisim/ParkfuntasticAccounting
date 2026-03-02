using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDotPartsWithAllFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseModelKey",
                table: "DotParts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseModelName",
                table: "DotParts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatProcessNumber",
                table: "DotParts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionIdentifier",
                table: "DotParts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerKey",
                table: "DotParts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerName",
                table: "DotParts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "DotParts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousPartNumbersJson",
                table: "DotParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousPricesJson",
                table: "DotParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriceDate",
                table: "DotParts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubModelsJson",
                table: "DotParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleType",
                table: "DotParts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleTypeName",
                table: "DotParts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkTimeMax",
                table: "DotParts",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkTimeMin",
                table: "DotParts",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseModelKey",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "BaseModelName",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "DatProcessNumber",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "DescriptionIdentifier",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "ManufacturerKey",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "ManufacturerName",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "PreviousPartNumbersJson",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "PreviousPricesJson",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "PriceDate",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "SubModelsJson",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "VehicleTypeName",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "WorkTimeMax",
                table: "DotParts");

            migrationBuilder.DropColumn(
                name: "WorkTimeMin",
                table: "DotParts");
        }
    }
}
