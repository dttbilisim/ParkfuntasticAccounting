#!/usr/bin/env dotnet script
#r "nuget: Npgsql, 9.0.1"

using Npgsql;

// appsettings'ten connection string al veya environment variable
var connStr = Environment.GetEnvironmentVariable("DB_CONNECTION") 
    ?? "Host=92.204.172.6;Port=5454;User ID=myinsurer;Password=Posmdh0738;Database=Parkfuntactic;Pooling=true;";

var sql = @"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CartItems' AND column_name = 'Voucher') THEN
        ALTER TABLE ""CartItems"" ADD COLUMN ""Voucher"" text NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CartItems' AND column_name = 'GuideName') THEN
        ALTER TABLE ""CartItems"" ADD COLUMN ""GuideName"" text NULL;
    END IF;
END $$;

INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
SELECT '20260309100000_AddVoucherAndGuideNameToCartItems', '9.0.6'
WHERE NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260309100000_AddVoucherAndGuideNameToCartItems');
";

await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand(sql, conn);
await cmd.ExecuteNonQueryAsync();
Console.WriteLine("CartItems Voucher ve GuideName kolonlari eklendi.");