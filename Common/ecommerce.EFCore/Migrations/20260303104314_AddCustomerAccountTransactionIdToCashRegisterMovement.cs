using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAccountTransactionIdToCashRegisterMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ParentCourierId",
                table: "AspNetUsers");

            // FK_CashRegisterMovements_SalesPersons_SalesPersonId - 20260303130000'de oluşturuluyor, bu migration'dan önce yok
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CashRegisterMovements_SalesPersons_SalesPersonId') THEN
                        ALTER TABLE ""CashRegisterMovements"" DROP CONSTRAINT ""FK_CashRegisterMovements_SalesPersons_SalesPersonId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierServiceAreas_CourierVehicles_CourierVehicleId",
                table: "CourierServiceAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierVehicles_AspNetUsers_DriverUserId",
                table: "CourierVehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CourierVehicles_CourierVehicleId",
                table: "Orders");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId_NeighboorId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId"";");

            migrationBuilder.AlterColumn<string>(
                name: "LicensePlate",
                table: "CourierVehicles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.Sql(@"
                ALTER TABLE ""CourierVehicles"" ADD COLUMN IF NOT EXISTS ""DriverName"" text;
                ALTER TABLE ""CourierVehicles"" ADD COLUMN IF NOT EXISTS ""DriverPhone"" text;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CashRegisterMovements"" ADD COLUMN IF NOT EXISTS ""CustomerAccountTransactionId"" integer NULL;
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId_NeighboorId"" 
                ON ""CourierServiceAreas"" (""CourierId"", ""CourierVehicleId"", ""CityId"", ""TownId"", ""NeighboorId"") 
                WHERE ""NeighboorId"" IS NOT NULL;
            ");
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId"" 
                ON ""CourierServiceAreas"" (""CourierId"", ""CourierVehicleId"", ""CityId"", ""TownId"") 
                WHERE ""NeighboorId"" IS NULL;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_CashRegisterMovements_CustomerAccountTransactionId"" ON ""CashRegisterMovements"" (""CustomerAccountTransactionId"");
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ParentCourierId",
                table: "AspNetUsers",
                column: "ParentCourierId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint c 
                        JOIN pg_class t ON c.conrelid = t.oid 
                        JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(c.conkey) AND a.attname = 'CustomerAccountTransactionId'
                        WHERE t.relname = 'CashRegisterMovements' AND c.contype = 'f') THEN
                        ALTER TABLE ""CashRegisterMovements"" ADD CONSTRAINT ""FK_CRM_CustomerAccountTransactionId""
                            FOREIGN KEY (""CustomerAccountTransactionId"") REFERENCES ""CustomerAccountTransactions"" (""Id"");
                    END IF;
                END $$;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierServiceAreas_CourierVehicles_CourierVehicleId",
                table: "CourierServiceAreas",
                column: "CourierVehicleId",
                principalTable: "CourierVehicles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierVehicles_AspNetUsers_DriverUserId",
                table: "CourierVehicles",
                column: "DriverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CourierVehicles_CourierVehicleId",
                table: "Orders",
                column: "CourierVehicleId",
                principalTable: "CourierVehicles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ParentCourierId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_CashRegisterMovements_CustomerAccountTransactions_CustomerA~",
                table: "CashRegisterMovements");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CashRegisterMovements_SalesPersons_SalesPersonId') THEN
                        ALTER TABLE ""CashRegisterMovements"" DROP CONSTRAINT ""FK_CashRegisterMovements_SalesPersons_SalesPersonId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierServiceAreas_CourierVehicles_CourierVehicleId",
                table: "CourierServiceAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_CourierVehicles_AspNetUsers_DriverUserId",
                table: "CourierVehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CourierVehicles_CourierVehicleId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownI~",
                table: "CourierServiceAreas");

            migrationBuilder.DropIndex(
                name: "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId",
                table: "CourierServiceAreas");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisterMovements_CustomerAccountTransactionId",
                table: "CashRegisterMovements");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "CourierVehicles");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "CourierVehicles");

            migrationBuilder.DropColumn(
                name: "CustomerAccountTransactionId",
                table: "CashRegisterMovements");

            migrationBuilder.AlterColumn<string>(
                name: "LicensePlate",
                table: "CourierVehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownI~",
                table: "CourierServiceAreas",
                columns: new[] { "CourierId", "CourierVehicleId", "CityId", "TownId", "NeighboorId" },
                unique: true,
                filter: "\"NeighboorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CourierServiceAreas_CourierId_CourierVehicleId_CityId_TownId",
                table: "CourierServiceAreas",
                columns: new[] { "CourierId", "CourierVehicleId", "CityId", "TownId" },
                unique: true,
                filter: "\"NeighboorId\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ParentCourierId",
                table: "AspNetUsers",
                column: "ParentCourierId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'CashRegisterMovements' AND column_name = 'SalesPersonId')
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CashRegisterMovements_SalesPersons_SalesPersonId') THEN
                        ALTER TABLE ""CashRegisterMovements"" ADD CONSTRAINT ""FK_CashRegisterMovements_SalesPersons_SalesPersonId""
                            FOREIGN KEY (""SalesPersonId"") REFERENCES ""SalesPersons"" (""Id"") ON DELETE SET NULL;
                    END IF;
                END $$;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_CourierServiceAreas_CourierVehicles_CourierVehicleId",
                table: "CourierServiceAreas",
                column: "CourierVehicleId",
                principalTable: "CourierVehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CourierVehicles_AspNetUsers_DriverUserId",
                table: "CourierVehicles",
                column: "DriverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CourierVehicles_CourierVehicleId",
                table: "Orders",
                column: "CourierVehicleId",
                principalTable: "CourierVehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
