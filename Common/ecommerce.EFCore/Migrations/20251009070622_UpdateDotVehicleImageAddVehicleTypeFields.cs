using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDotVehicleImageAddVehicleTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DatECode",
                table: "DotVehicleImages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "BaseModelKey",
                table: "DotVehicleImages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerKey",
                table: "DotVehicleImages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubModelKey",
                table: "DotVehicleImages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleType",
                table: "DotVehicleImages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseModelKey",
                table: "DotVehicleImages");

            migrationBuilder.DropColumn(
                name: "ManufacturerKey",
                table: "DotVehicleImages");

            migrationBuilder.DropColumn(
                name: "SubModelKey",
                table: "DotVehicleImages");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "DotVehicleImages");

            migrationBuilder.AlterColumn<string>(
                name: "DatECode",
                table: "DotVehicleImages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
