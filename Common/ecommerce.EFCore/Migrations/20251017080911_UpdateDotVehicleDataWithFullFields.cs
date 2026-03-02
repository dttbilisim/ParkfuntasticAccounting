using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDotVehicleDataWithFullFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Acceleration",
                table: "DotVehicleData",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Co2Emission",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Consumption",
                table: "DotVehicleData",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsumptionInTown",
                table: "DotVehicleData",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsumptionOutOfTown",
                table: "DotVehicleData",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerName",
                table: "DotVehicleData",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountOfAirbags",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountOfAxles",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountOfDrivedAxles",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cylinder",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CylinderArrangement",
                table: "DotVehicleData",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Drive",
                table: "DotVehicleData",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveCode",
                table: "DotVehicleData",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmissionClass",
                table: "DotVehicleData",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EngineCycle",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelMethodCode",
                table: "DotVehicleData",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelMethodType",
                table: "DotVehicleData",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsuranceTypeClassCascoComplete",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsuranceTypeClassCascoPartial",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsuranceTypeClassLiability",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KbaNumbers",
                table: "DotVehicleData",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoadingSpace",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoadingSpaceMax",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NrOfGears",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalPriceVATRate",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalTireSizeAxle1",
                table: "DotVehicleData",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalTireSizeAxle2",
                table: "DotVehicleData",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PermissableTotalWeight",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductGroupName",
                table: "DotVehicleData",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RentalCarClass",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoofLoad",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RotationsOnMaxPower",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RotationsOnMaxTorque",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpeedMax",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructureDescription",
                table: "DotVehicleData",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructureType",
                table: "DotVehicleData",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TankVolume",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Torque",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrailerLoadBraked",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrailerLoadUnbraked",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnloadedWeight",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleDoors",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleSeats",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WheelBase",
                table: "DotVehicleData",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Acceleration",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "Co2Emission",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "Consumption",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "ConsumptionInTown",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "ConsumptionOutOfTown",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "ContainerName",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "CountOfAirbags",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "CountOfAxles",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "CountOfDrivedAxles",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "Cylinder",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "CylinderArrangement",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "Drive",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "DriveCode",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "EmissionClass",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "EngineCycle",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "FuelMethodCode",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "FuelMethodType",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "InsuranceTypeClassCascoComplete",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "InsuranceTypeClassCascoPartial",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "InsuranceTypeClassLiability",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "KbaNumbers",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "LoadingSpace",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "LoadingSpaceMax",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "NrOfGears",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "OriginalPriceVATRate",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "OriginalTireSizeAxle1",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "OriginalTireSizeAxle2",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "PermissableTotalWeight",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "ProductGroupName",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "RentalCarClass",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "RoofLoad",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "RotationsOnMaxPower",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "RotationsOnMaxTorque",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "SpeedMax",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "StructureDescription",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "StructureType",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "TankVolume",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "Torque",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "TrailerLoadBraked",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "TrailerLoadUnbraked",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "UnloadedWeight",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "VehicleDoors",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "VehicleSeats",
                table: "DotVehicleData");

            migrationBuilder.DropColumn(
                name: "WheelBase",
                table: "DotVehicleData");
        }
    }
}
