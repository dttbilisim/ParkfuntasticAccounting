using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserCarsWithDotIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_CarBrands_CarBrandId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_CarEngines_CarEngineId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_CarFuelTypes_CarFuelTypeId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_CarModels_CarModelId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_CarYears_CarYearId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_CarBrandId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_CarEngineId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_CarFuelTypeId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_CarModelId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_CarYearId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "CarBrandId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "CarEngineId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "CarFuelTypeId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "CarModelId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "CarYearId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "VIN",
                table: "UserCars");

            migrationBuilder.AddColumn<int>(
                name: "DotBaseModelId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DotBaseModelKey",
                table: "UserCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DotCarBodyOptionId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DotCompiledCodeId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DotDatECode",
                table: "UserCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DotEngineOptionId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DotManufacturerId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DotManufacturerKey",
                table: "UserCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DotOptionId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DotSubModelId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DotSubModelKey",
                table: "UserCars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DotVehicleTypeId",
                table: "UserCars",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_DotBaseModelId",
                table: "UserCars",
                column: "DotBaseModelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_DotCarBodyOptionId",
                table: "UserCars",
                column: "DotCarBodyOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_DotCompiledCodeId",
                table: "UserCars",
                column: "DotCompiledCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_DotEngineOptionId",
                table: "UserCars",
                column: "DotEngineOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_DotManufacturerId",
                table: "UserCars",
                column: "DotManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_DotOptionId",
                table: "UserCars",
                column: "DotOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_DotSubModelId",
                table: "UserCars",
                column: "DotSubModelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_DotVehicleTypeId",
                table: "UserCars",
                column: "DotVehicleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_DotBaseModels_DotBaseModelId",
                table: "UserCars",
                column: "DotBaseModelId",
                principalTable: "DotBaseModels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_DotCarBodyOptions_DotCarBodyOptionId",
                table: "UserCars",
                column: "DotCarBodyOptionId",
                principalTable: "DotCarBodyOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_DotCompiledCodes_DotCompiledCodeId",
                table: "UserCars",
                column: "DotCompiledCodeId",
                principalTable: "DotCompiledCodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_DotEngineOptions_DotEngineOptionId",
                table: "UserCars",
                column: "DotEngineOptionId",
                principalTable: "DotEngineOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_DotManufacturers_DotManufacturerId",
                table: "UserCars",
                column: "DotManufacturerId",
                principalTable: "DotManufacturers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_DotOptions_DotOptionId",
                table: "UserCars",
                column: "DotOptionId",
                principalTable: "DotOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_DotSubModels_DotSubModelId",
                table: "UserCars",
                column: "DotSubModelId",
                principalTable: "DotSubModels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_DotVehicleTypes_DotVehicleTypeId",
                table: "UserCars",
                column: "DotVehicleTypeId",
                principalTable: "DotVehicleTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_DotBaseModels_DotBaseModelId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_DotCarBodyOptions_DotCarBodyOptionId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_DotCompiledCodes_DotCompiledCodeId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_DotEngineOptions_DotEngineOptionId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_DotManufacturers_DotManufacturerId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_DotOptions_DotOptionId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_DotSubModels_DotSubModelId",
                table: "UserCars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCars_DotVehicleTypes_DotVehicleTypeId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_DotBaseModelId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_DotCarBodyOptionId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_DotCompiledCodeId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_DotEngineOptionId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_DotManufacturerId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_DotOptionId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_DotSubModelId",
                table: "UserCars");

            migrationBuilder.DropIndex(
                name: "IX_UserCars_DotVehicleTypeId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotBaseModelId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotBaseModelKey",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotCarBodyOptionId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotCompiledCodeId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotDatECode",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotEngineOptionId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotManufacturerId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotManufacturerKey",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotOptionId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotSubModelId",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotSubModelKey",
                table: "UserCars");

            migrationBuilder.DropColumn(
                name: "DotVehicleTypeId",
                table: "UserCars");

            migrationBuilder.AddColumn<int>(
                name: "CarBrandId",
                table: "UserCars",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CarEngineId",
                table: "UserCars",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CarFuelTypeId",
                table: "UserCars",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CarModelId",
                table: "UserCars",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CarYearId",
                table: "UserCars",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VIN",
                table: "UserCars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_CarBrandId",
                table: "UserCars",
                column: "CarBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_CarEngineId",
                table: "UserCars",
                column: "CarEngineId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_CarFuelTypeId",
                table: "UserCars",
                column: "CarFuelTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_CarModelId",
                table: "UserCars",
                column: "CarModelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCars_CarYearId",
                table: "UserCars",
                column: "CarYearId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_CarBrands_CarBrandId",
                table: "UserCars",
                column: "CarBrandId",
                principalTable: "CarBrands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_CarEngines_CarEngineId",
                table: "UserCars",
                column: "CarEngineId",
                principalTable: "CarEngines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_CarFuelTypes_CarFuelTypeId",
                table: "UserCars",
                column: "CarFuelTypeId",
                principalTable: "CarFuelTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_CarModels_CarModelId",
                table: "UserCars",
                column: "CarModelId",
                principalTable: "CarModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCars_CarYears_CarYearId",
                table: "UserCars",
                column: "CarYearId",
                principalTable: "CarYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
