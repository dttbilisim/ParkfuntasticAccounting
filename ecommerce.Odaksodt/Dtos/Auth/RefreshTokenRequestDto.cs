namespace ecommerce.Odaksodt.Dtos.Auth;

/// <summary>
/// Refresh token request DTO
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
