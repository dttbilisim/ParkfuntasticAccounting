namespace ecommerce.EP.Services.Abstract;

/// <summary>
/// Eski ve geçersiz push token'ları temizleyen servis arayüzü.
/// 30 günden eski token'ları pasif olarak işaretler.
/// </summary>
public interface ITokenCleanupService
{
    /// <summary>
    /// UpdatedAt tarihi 30 günden eski olan aktif token'ları IsActive = false olarak işaretler.
    /// </summary>
    /// <returns>Pasif olarak işaretlenen token sayısı</returns>
    Task<int> CleanupExpiredTokensAsync();
}
