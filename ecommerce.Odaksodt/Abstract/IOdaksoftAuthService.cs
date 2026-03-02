using ecommerce.Odaksodt.Dtos.Auth;

namespace ecommerce.Odaksodt.Abstract;

/// <summary>
/// Odaksoft kimlik doğrulama servisi interface
/// </summary>
public interface IOdaksoftAuthService
{
    /// <summary>
    /// Kullanıcı girişi yapar ve token alır
    /// </summary>
    Task<LoginResponseDto> LoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Token'ı yeniler
    /// </summary>
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Geçerli token'ı döndürür (gerekirse otomatik yeniler)
    /// </summary>
    Task<string> GetValidTokenAsync(CancellationToken cancellationToken = default);
}
