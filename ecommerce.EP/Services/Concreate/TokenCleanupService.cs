using ecommerce.EFCore.Context;
using ecommerce.EP.Services.Abstract;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.EP.Services.Concreate;

/// <summary>
/// 30 günden eski push token'ları IsActive = false olarak işaretleyen servis.
/// Toplu güncelleme için ExecuteUpdateAsync kullanır (performans).
/// </summary>
public class TokenCleanupService : ITokenCleanupService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TokenCleanupService> _logger;

    /// <summary>Token'ın eski sayılması için gün eşiği</summary>
    private const int ExpirationDays = 30;

    public TokenCleanupService(
        ApplicationDbContext dbContext,
        ILogger<TokenCleanupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var thresholdDate = DateTime.UtcNow.AddDays(-ExpirationDays);

            // Sadece aktif olan ve 30 günden eski token'ları güncelle
            var updatedCount = await _dbContext.UserPushTokens
                .Where(t => t.IsActive && t.UpdatedAt < thresholdDate)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.IsActive, false));

            if (updatedCount > 0)
            {
                _logger.LogInformation(
                    "Eski token temizleme tamamlandı. {UpdatedCount} token pasif olarak işaretlendi. Eşik tarihi: {ThresholdDate:yyyy-MM-dd HH:mm:ss}",
                    updatedCount, thresholdDate);
            }
            else
            {
                _logger.LogInformation(
                    "Eski token temizleme çalıştı, temizlenecek token bulunamadı. Eşik tarihi: {ThresholdDate:yyyy-MM-dd HH:mm:ss}",
                    thresholdDate);
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eski token temizleme sırasında hata oluştu.");
            throw;
        }
    }
}
