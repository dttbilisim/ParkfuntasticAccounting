namespace ecommerce.Cargo.Mng;

public class MngOptions
{
    public bool UseSandbox { get; set; }

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string CustomerNumber { get; set; } = null!;

    public string Password { get; set; } = null!;
}