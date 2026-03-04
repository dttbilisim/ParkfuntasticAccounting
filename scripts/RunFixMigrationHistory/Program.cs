// Migration history düzeltme script'i - TÜM migration'ları history'ye ekler
// Kullanım: dotnet run --project Scripts/RunFixMigrationHistory

using Npgsql;

var connStr = "Host=92.204.172.6;Port=5454;User ID=myinsurer;Password=Posmdh0738;Database=Parkfuntactic";

// Migrations klasöründen tüm migration ID'lerini oku (dotnet run solution root'tan çalışır)
var migrationsDir = Path.Combine(Directory.GetCurrentDirectory(), "Common", "ecommerce.EFCore", "Migrations");
var migrations = Directory.GetFiles(migrationsDir, "*.cs")
    .Where(f => !f.Contains("Designer") && !f.Contains("Snapshot"))
    .Select(f => Path.GetFileNameWithoutExtension(f))
    .OrderBy(x => x)
    .Select(id => (id, "9.0.6"))
    .ToList();

try
{
    await using var conn = new NpgsqlConnection(connStr);
    await conn.OpenAsync();
    Console.WriteLine($"{migrations.Count} migration ekleniyor...");
    foreach (var (id, ver) in migrations)
    {
        try
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@id, @ver)", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("ver", ver);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Eklendi: {id}");
        }
        catch (Npgsql.PostgresException pe) when (pe.SqlState == "23505") { /* duplicate - zaten var */ }
    }
    Console.WriteLine("Migration history güncellendi. Şimdi şunu çalıştır:");
    Console.WriteLine("dotnet ef database update --project Common/ecommerce.EFCore --startup-project ecommerce.Admin --context ApplicationDbContext");
}
catch (Exception ex)
{
    Console.WriteLine("Hata: " + ex.Message);
    return 1;
}
return 0;
