using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>Onaylanmış kurye yönetimi ve hizmet bölgeleri.</summary>
public interface ICourierService
{
    Task<IActionResult<CourierDetailDto>> GetById(int id);
    /// <summary>ApplicationUserId ile kurye kaydını getirir (kurye paneli için).</summary>
    Task<IActionResult<CourierDetailDto?>> GetByApplicationUserId(int applicationUserId);
    /// <summary>Kurye kaydı yoksa oluşturur (Courier rolü olan kullanıcı ilk konum gönderdiğinde).</summary>
    Task<IActionResult<CourierDetailDto>> GetOrCreateByApplicationUserId(int applicationUserId);
    Task<IActionResult<Paging<List<CourierListDto>>>> GetPaged(PageSetting pager, int? status = null);
    Task<IActionResult<List<CourierVehicleListDto>>> GetVehicles(int courierId);
    /// <summary>Araç ekler veya günceller. Id null ise yeni araç. parentCourierApplicationUserId verilirse DriverUserId alt kullanıcı olarak doğrulanır.</summary>
    Task<IActionResult<CourierVehicleListDto>> SaveVehicle(int courierId, CourierVehicleUpsertDto dto, int? parentCourierApplicationUserId = null);
    Task<IActionResult<Empty>> DeleteVehicle(int courierId, int vehicleId);
    Task<IActionResult<List<CourierServiceAreaListDto>>> GetServiceAreas(int courierId);
    /// <summary>Kuryenin hizmet bölgelerini güncelle (liste ile replace).</summary>
    Task<IActionResult<Empty>> SaveServiceAreas(int courierId, List<CourierServiceAreaUpsertDto> areas);
}
