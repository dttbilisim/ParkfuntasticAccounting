using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos;
using ecommerce.Core.Identity;

namespace ecommerce.EP.Controllers;

/// <summary>
/// Kullanıcı tema tercihleri yönetimi
/// </summary>
[Route("api/user/theme")]
[ApiController]
[Authorize]
public class UserThemeController : ControllerBase
{
    private readonly IRedisCacheService _redisCacheService;
    private readonly CurrentUser _currentUser;

    public UserThemeController(
        IRedisCacheService redisCacheService,
        CurrentUser currentUser)
    {
        _redisCacheService = redisCacheService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Kullanıcının tema tercihini getirir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserThemeDto>> GetTheme()
    {
        var userId = _currentUser.UserId;
        if (userId == null || userId <= 0)
        {
            return Unauthorized();
        }

        var cacheKey = $"user:theme:{userId}";
        var theme = await _redisCacheService.GetAsync<string>(cacheKey);

        return Ok(new UserThemeDto
        {
            Theme = theme ?? "purple" // Varsayılan tema
        });
    }

    /// <summary>
    /// Kullanıcının tema tercihini kaydeder
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SetTheme([FromBody] UserThemeDto dto)
    {
        var userId = _currentUser.UserId;
        if (userId == null || userId <= 0)
        {
            return Unauthorized();
        }

        // Tema validasyonu
        var validThemes = new[] { "purple", "ocean", "forest", "sunset" };
        var themeName = dto.Theme?.ToLower() ?? "";
        if (!validThemes.Contains(themeName))
        {
            return BadRequest(new { message = "Geçersiz tema. Geçerli temalar: purple, ocean, forest, sunset" });
        }

        var cacheKey = $"user:theme:{userId}";
        await _redisCacheService.SetAsync(cacheKey, themeName, TimeSpan.FromDays(365));

        return Ok(new { message = "Tema tercihi kaydedildi", theme = themeName });
    }
}
