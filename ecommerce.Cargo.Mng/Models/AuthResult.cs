namespace ecommerce.Cargo.Mng.Models;

public class AuthResult
{
    public string Jwt { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime JwtExpireDate { get; set; }

    public DateTime RefreshTokenExpireDate { get; set; }
}