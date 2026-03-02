namespace ecommerce.Odaksodt.Dtos.Auth;

/// <summary>
/// Login request DTO
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Şifre
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
