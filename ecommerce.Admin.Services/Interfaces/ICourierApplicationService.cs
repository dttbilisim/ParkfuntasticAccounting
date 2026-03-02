using ecommerce.Admin.Domain.Dtos.CourierApplicationDto;
using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>Kurye başvurusu: mobil başvuru, admin liste/onay/red.</summary>
public interface ICourierApplicationService
{
    /// <summary>Mobil: Kullanıcı kurye başvurusu yapar (ApplicationUserId çağırandan alınır).</summary>
    Task<IActionResult<int>> Create(int applicationUserId, CourierApplicationUpsertDto dto);

    /// <summary>Admin: Başvuruları sayfalı listele.</summary>
    Task<IActionResult<Paging<List<CourierApplicationListDto>>>> GetPaged(PageSetting pager, CourierApplicationStatus? status = null);

    /// <summary>Admin: Başvuruyu onayla veya reddet. Onayda Courier kaydı oluşturulur.</summary>
    Task<IActionResult<Empty>> Review(int applicationId, CourierApplicationReviewDto dto, int reviewedByUserId);

    /// <summary>Admin: Başvuruyu tamamen siler (kayıt ve ilişkili veriler).</summary>
    Task<IActionResult<Empty>> Delete(int applicationId);
}
