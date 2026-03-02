namespace ecommerce.Domain.Shared.Options;
public class RedisOptions{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6380;
    public string? Password { get; set; }
    public int DefaultDatabase { get; set; } = 0;
    public bool Ssl { get; set; } = false;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public int AsyncTimeout { get; set; } = 5000;
}
