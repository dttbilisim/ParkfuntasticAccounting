using System.Security.Claims;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using ecommerce.EP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.EP.Controllers
{
    /// <summary>
    /// Push bildirim token yönetimi ve kullanıcı bildirim geçmişi controller'ı.
    /// Token kayıt (upsert), silme ve kullanıcı bildirim listesi işlemlerini yönetir.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            ApplicationDbContext dbContext,
            ILogger<NotificationController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcının bildirim listesini sayfalı olarak getirir.
        /// Soft delete edilmemiş bildirimleri yeniden eskiye sıralar.
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var query = _dbContext.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsDeleted);

            var totalCount = await query.CountAsync();
            var unreadCount = await query.CountAsync(n => !n.IsRead);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new UserNotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Body = n.Body,
                    DeepLink = n.DeepLink,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync();

            return Ok(new UserNotificationListResponse
            {
                Items = items,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Okunmamış bildirim sayısını döndürür — badge gösterimi için.
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var count = await _dbContext.UserNotifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsDeleted && !n.IsRead);

            return Ok(new { unreadCount = count });
        }

        /// <summary>
        /// Tek bir bildirimi okundu olarak işaretler.
        /// </summary>
        [HttpPatch("{id}/read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var notification = await _dbContext.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

            if (notification == null)
                return NotFound(new { message = "Bildirim bulunamadı." });

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { message = "Bildirim okundu olarak işaretlendi." });
        }

        /// <summary>
        /// Tüm bildirimleri okundu olarak işaretler.
        /// </summary>
        [HttpPatch("read-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var now = DateTime.UtcNow;
            await _dbContext.UserNotifications
                .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now));

            return Ok(new { message = "Tüm bildirimler okundu olarak işaretlendi." });
        }

        /// <summary>
        /// Tek bir bildirimi siler (soft delete).
        /// Gerçek cihazlarda CORS/DELETE sorunları nedeniyle POST kullanılıyor.
        /// </summary>
        [HttpPost("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var notification = await _dbContext.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

            if (notification == null)
                return NotFound(new { message = "Bildirim bulunamadı." });

            notification.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Bildirim silindi." });
        }

        /// <summary>
        /// Push token kaydı — upsert mantığı.
        /// Aynı DeviceId varsa günceller, yoksa yeni kayıt oluşturur.
        /// </summary>
        [HttpPost("register-token")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenOperationResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenRequest request)
        {
            // Model validasyonu otomatik çalışır ([Required], [RegularExpression])
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // JWT claim'lerinden UserId al
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new TokenOperationResponse
                {
                    Success = false,
                    Message = "Kullanıcı kimliği bulunamadı."
                });
            }

            try
            {
                // Aynı Token string'ine sahip eski kayıtları temizle (farklı DeviceId ile mükerrer kayıt önleme)
                var duplicateTokenRecords = await _dbContext.UserPushTokens
                    .Where(t => t.UserId == userId && t.Token == request.Token && t.DeviceId != request.DeviceId)
                    .ToListAsync();

                if (duplicateTokenRecords.Count > 0)
                {
                    _dbContext.UserPushTokens.RemoveRange(duplicateTokenRecords);
                    _logger.LogInformation(
                        "Mükerrer token kayıtları temizlendi. UserId: {UserId}, Silinen: {Count}",
                        userId, duplicateTokenRecords.Count);
                }

                // Aynı DeviceId ile mevcut kayıt var mı kontrol et
                var existingToken = await _dbContext.UserPushTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == request.DeviceId);

                if (existingToken != null)
                {
                    // Mevcut kaydı güncelle
                    existingToken.Token = request.Token;
                    existingToken.Platform = request.Platform;
                    existingToken.UpdatedAt = DateTime.UtcNow;
                    existingToken.IsActive = true;

                    _dbContext.UserPushTokens.Update(existingToken);

                    _logger.LogInformation(
                        "Push token güncellendi. UserId: {UserId}, DeviceId: {DeviceId}, Platform: {Platform}",
                        userId, request.DeviceId, request.Platform);
                }
                else
                {
                    // Yeni kayıt oluştur
                    var newToken = new UserPushToken
                    {
                        UserId = userId,
                        Token = request.Token,
                        Platform = request.Platform,
                        DeviceId = request.DeviceId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _dbContext.UserPushTokens.AddAsync(newToken);

                    _logger.LogInformation(
                        "Yeni push token kaydedildi. UserId: {UserId}, DeviceId: {DeviceId}, Platform: {Platform}",
                        userId, request.DeviceId, request.Platform);
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new TokenOperationResponse
                {
                    Success = true,
                    Message = "Token başarıyla kaydedildi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Push token kayıt hatası. UserId: {UserId}, DeviceId: {DeviceId}",
                    userId, request.DeviceId);

                return StatusCode(StatusCodes.Status500InternalServerError, new TokenOperationResponse
                {
                    Success = false,
                    Message = "Token kaydedilirken bir hata oluştu."
                });
            }
        }

        /// <summary>
        /// Push token silme — belirtilen DeviceId'ye ait token'ı siler.
        /// </summary>
        [HttpDelete("unregister-token/{deviceId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenOperationResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UnregisterToken(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return BadRequest(new TokenOperationResponse
                {
                    Success = false,
                    Message = "DeviceId boş olamaz."
                });
            }

            // JWT claim'lerinden UserId al
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new TokenOperationResponse
                {
                    Success = false,
                    Message = "Kullanıcı kimliği bulunamadı."
                });
            }

            try
            {
                // UserId + DeviceId ile kaydı bul
                var existingToken = await _dbContext.UserPushTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == deviceId);

                if (existingToken == null)
                {
                    _logger.LogWarning(
                        "Silinecek push token bulunamadı. UserId: {UserId}, DeviceId: {DeviceId}",
                        userId, deviceId);

                    return NotFound(new TokenOperationResponse
                    {
                        Success = false,
                        Message = "Token bulunamadı."
                    });
                }

                _dbContext.UserPushTokens.Remove(existingToken);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Push token silindi. UserId: {UserId}, DeviceId: {DeviceId}, Platform: {Platform}",
                    userId, deviceId, existingToken.Platform);

                return Ok(new TokenOperationResponse
                {
                    Success = true,
                    Message = "Token başarıyla silindi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Push token silme hatası. UserId: {UserId}, DeviceId: {DeviceId}",
                    userId, deviceId);

                return StatusCode(StatusCodes.Status500InternalServerError, new TokenOperationResponse
                {
                    Success = false,
                    Message = "Token silinirken bir hata oluştu."
                });
            }
        }
    }
}
