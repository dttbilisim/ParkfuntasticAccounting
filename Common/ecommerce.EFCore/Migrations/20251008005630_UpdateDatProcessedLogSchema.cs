using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatProcessedLogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityKey",
                table: "DatProcessedLogs");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "DatProcessedLogs",
                newName: "LastSyncDate");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "DatProcessedLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstSyncDate",
                table: "DatProcessedLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "DatProcessedLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastProcessedEntityId",
                table: "DatProcessedLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProcessedKey",
                table: "DatProcessedLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalProcessed",
                table: "DatProcessedLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstSyncDate",
                table: "DatProcessedLogs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "DatProcessedLogs");

            migrationBuilder.DropColumn(
                name: "LastProcessedEntityId",
                table: "DatProcessedLogs");

            migrationBuilder.DropColumn(
                name: "LastProcessedKey",
                table: "DatProcessedLogs");

            migrationBuilder.DropColumn(
                name: "TotalProcessed",
                table: "DatProcessedLogs");

            migrationBuilder.RenameColumn(
                name: "LastSyncDate",
                table: "DatProcessedLogs",
                newName: "ProcessedAt");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "DatProcessedLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "EntityKey",
                table: "DatProcessedLogs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
