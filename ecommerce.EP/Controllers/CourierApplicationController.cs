using System.Security.Claims;
using ecommerce.Admin.Domain.Dtos.CourierApplicationDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.EP.Controllers;

/// <summary>Mobil kurye başvurusu — giriş yapmış kullanıcı başvuru yapar.</summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CourierApplicationController : ControllerBase
{
    private readonly ICourierApplicationService _applicationService;
    private readonly ILogger<CourierApplicationController> _logger;

    public CourierApplicationController(
        ICourierApplicationService applicationService,
        ILogger<CourierApplicationController> logger)
    {
        _applicationService = applicationService;
        _logger = logger;
    }

    /// <summary>
    /// Kurye başvurusu oluşturur. ApplicationUserId JWT'den alınır.
    /// </summary>
    /// <param name="dto">Telefon zorunlu; TC ve not opsiyonel.</param>
    /// <returns>Oluşturulan başvuru Id</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CourierApplicationUpsertDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var applicationUserId))
        {
            return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });
        }

        var result = await _applicationService.Create(applicationUserId, dto);
        if (!result.Ok)
        {
            return BadRequest(new { message = result.GetMetadataMessages() });
        }

        return Ok(new { applicationId = result.Result, message = "Başvurunuz alındı. İnceleme sonucu bildirilecektir." });
    }
}
