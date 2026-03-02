using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Extensions;
using ecommerce.Core.Utils;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate;

public class CourierService : ICourierService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CourierService> _logger;

    public CourierService(
        IUnitOfWork<ApplicationDbContext> context,
        IServiceScopeFactory scopeFactory,
        ILogger<CourierService> logger)
    {
        _context = context;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<IActionResult<CourierDetailDto>> GetById(int id)
    {
        var result = new IActionResult<CourierDetailDto>();
        try
        {
            var courier = await _context.DbContext.Couriers
                .AsNoTracking()
                .Include(x => x.ApplicationUser)
                .Include(x => x.ServiceAreas).ThenInclude(s => s.City)
                .Include(x => x.ServiceAreas).ThenInclude(s => s.Town)
                .Include(x => x.ServiceAreas).ThenInclude(s => s.Neighboor)
                .Include(x => x.ServiceAreas).ThenInclude(s => s.CourierVehicle)
                .Include(x => x.Vehicles)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (courier == null)
            {
                result.AddError("Kurye bulunamadı.");
                return result;
            }
            result.Result = new CourierDetailDto
            {
                Id = courier.Id,
                ApplicationUserId = courier.ApplicationUserId,
                UserName = courier.ApplicationUser?.FullName ?? "",
                Email = courier.ApplicationUser?.Email,
                Phone = courier.ApplicationUser?.PhoneNumber,
                Status = courier.Status,
                StatusName = ((EntityStatus)courier.Status).GetDisplayName(),
                CreatedDate = courier.CreatedDate,
                ServiceAreas = courier.ServiceAreas?.Select(s => new CourierServiceAreaListDto
                {
                    Id = s.Id,
                    CourierId = s.CourierId,
                    CourierVehicleId = s.CourierVehicleId,
                    VehicleDisplay = s.CourierVehicle != null ? $"{((CourierVehicleType)s.CourierVehicle.VehicleType).GetDisplayName()} - {s.CourierVehicle.LicensePlate}" : null,
                    CityId = s.CityId,
                    CityName = s.City?.Name ?? "",
                    TownId = s.TownId,
                    TownName = s.Town?.Name ?? "",
                    NeighboorId = s.NeighboorId,
                    NeighboorName = s.Neighboor?.Name,
                    WorkStartTime = s.WorkStartTime?.ToString("HH:mm"),
                    WorkEndTime = s.WorkEndTime?.ToString("HH:mm"),
                    IsActive = s.IsActive
                }).ToList() ?? new List<CourierServiceAreaListDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier GetById Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<CourierDetailDto?>> GetByApplicationUserId(int applicationUserId)
    {
        var result = new IActionResult<CourierDetailDto?> { Result = null };
        try
        {
            // Sadece Couriers + ApplicationUser: CourierServiceAreas/CourierVehicleId kolonu migration uygulanmamış DB'de yoksa hata vermemek için ServiceAreas yüklenmez
            var courier = await _context.DbContext.Couriers
                .AsNoTracking()
                .Include(x => x.ApplicationUser)
                .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);
            if (courier == null)
                return result;
            result.Result = new CourierDetailDto
            {
                Id = courier.Id,
                ApplicationUserId = courier.ApplicationUserId,
                UserName = courier.ApplicationUser?.FullName ?? "",
                Email = courier.ApplicationUser?.Email,
                Phone = courier.ApplicationUser?.PhoneNumber,
                Status = courier.Status,
                StatusName = ((EntityStatus)courier.Status).GetDisplayName(),
                CreatedDate = courier.CreatedDate,
                ServiceAreas = new List<CourierServiceAreaListDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier GetByApplicationUserId Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<CourierDetailDto>> GetOrCreateByApplicationUserId(int applicationUserId)
    {
        var existing = await GetByApplicationUserId(applicationUserId);
        if (existing.Ok && existing.Result != null)
            return new IActionResult<CourierDetailDto> { Result = existing.Result };

        try
        {
            var courier = new Courier
            {
                ApplicationUserId = applicationUserId,
                Status = (int)EntityStatus.Active,
                CreatedDate = DateTime.UtcNow,
                CreatedId = applicationUserId
            };
            _context.DbContext.Couriers.Add(courier);
            await _context.DbContext.SaveChangesAsync();

            _logger.LogInformation("Courier kaydı otomatik oluşturuldu: ApplicationUserId={UserId}, CourierId={CourierId}", applicationUserId, courier.Id);

            // Insert sonrası ikinci sorgu yerine eklenen kayıttan minimal DTO döndür (Include(Vehicles) vb. migration/veritabanı hatası riskini önler)
            return new IActionResult<CourierDetailDto>
            {
                Result = new CourierDetailDto
                {
                    Id = courier.Id,
                    ApplicationUserId = courier.ApplicationUserId,
                    UserName = "",
                    Email = null,
                    Phone = null,
                    Status = courier.Status,
                    StatusName = ((EntityStatus)courier.Status).GetDisplayName(),
                    CreatedDate = courier.CreatedDate,
                    ServiceAreas = new List<CourierServiceAreaListDto>()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier GetOrCreateByApplicationUserId Exception");
            var err = new IActionResult<CourierDetailDto>();
            err.AddSystemError(ex.Message);
            return err;
        }
    }

    public async Task<IActionResult<Paging<List<CourierListDto>>>> GetPaged(PageSetting pager, int? status = null)
    {
        var result = new IActionResult<Paging<List<CourierListDto>>>
        {
            Result = new Paging<List<CourierListDto>> { Data = new List<CourierListDto>(), DataCount = 0 }
        };
        try
        {
            // Ayrı scope/context kullan: aynı request içinde layout/tenant sorguları ile çakışmayı önler (Npgsql "A command is already in progress")
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>().DbContext;
            var query = db.Couriers
                .AsNoTracking()
                .Include(x => x.ApplicationUser)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            var totalCount = await query.CountAsync();
            var skip = pager.Skip ?? 0;
            var take = Math.Min(pager.Take ?? 25, 500);

            var list = await query
                .OrderByDescending(x => x.CreatedDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var courierIds = list.Select(x => x.Id).ToList();
            var areaCounts = await db.CourierServiceAreas
                .AsNoTracking()
                .Where(x => courierIds.Contains(x.CourierId))
                .GroupBy(x => x.CourierId)
                .Select(g => new { CourierId = g.Key, Count = g.Count() })
                .ToListAsync();
            var countDict = areaCounts.ToDictionary(x => x.CourierId, x => x.Count);

            var dtos = list.Select(x => new CourierListDto
            {
                Id = x.Id,
                ApplicationUserId = x.ApplicationUserId,
                UserName = x.ApplicationUser?.FullName ?? "",
                Email = x.ApplicationUser?.Email,
                Phone = x.ApplicationUser?.PhoneNumber,
                Status = x.Status,
                StatusName = ((EntityStatus)x.Status).GetDisplayName(),
                CreatedDate = x.CreatedDate,
                ServiceAreaCount = countDict.GetValueOrDefault(x.Id, 0)
            }).ToList();

            result.Result = new Paging<List<CourierListDto>>
            {
                Data = dtos,
                DataCount = totalCount,
                TotalRawCount = totalCount,
                CurrentPage = take > 0 ? (skip / take) + 1 : 1,
                PageSize = take
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier GetPaged Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<List<CourierVehicleListDto>>> GetVehicles(int courierId)
    {
        var result = new IActionResult<List<CourierVehicleListDto>> { Result = new List<CourierVehicleListDto>() };
        try
        {
            var list = await _context.DbContext.CourierVehicles
                .AsNoTracking()
                .Where(x => x.CourierId == courierId)
                .OrderBy(x => x.VehicleType).ThenBy(x => x.LicensePlate)
                .ToListAsync();
            result.Result = list.Select(v => new CourierVehicleListDto
            {
                Id = v.Id,
                CourierId = v.CourierId,
                VehicleType = v.VehicleType,
                VehicleTypeName = v.VehicleType.GetDisplayName(),
                LicensePlate = v.LicensePlate,
                DriverName = v.DriverName,
                DriverPhone = v.DriverPhone,
                DriverUserId = v.DriverUserId
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier GetVehicles Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<CourierVehicleListDto>> SaveVehicle(int courierId, CourierVehicleUpsertDto dto, int? parentCourierApplicationUserId = null)
    {
        var result = new IActionResult<CourierVehicleListDto>();
        try
        {
            if (string.IsNullOrWhiteSpace(dto.LicensePlate))
            {
                result.AddError("Plaka zorunludur.");
                return result;
            }
            string? driverName = dto.DriverName?.Trim();
            string? driverPhone = dto.DriverPhone?.Trim();
            int? driverUserId = dto.DriverUserId;
            if (driverUserId.HasValue && parentCourierApplicationUserId.HasValue)
            {
                ApplicationUser? driverUser = null;
                if (driverUserId.Value == parentCourierApplicationUserId.Value)
                {
                    // Ana kurye kendini şoför atıyor
                    driverUser = await _context.DbContext.AspNetUsers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == driverUserId.Value);
                }
                else
                {
                    // Alt kurye: sadece kendi eklediği alt kullanıcıları atayabilir
                    driverUser = await _context.DbContext.AspNetUsers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == driverUserId.Value && u.ParentCourierId == parentCourierApplicationUserId.Value);
                }
                if (driverUser == null)
                {
                    result.AddError("Seçilen kurye geçerli değil veya sizin alt kuryeniz değil.");
                    return result;
                }
                driverName = (driverUser.FirstName + " " + driverUser.LastName).Trim();
                driverPhone = driverUser.PhoneNumber?.Trim();
            }
            if (dto.Id.HasValue && dto.Id.Value > 0)
            {
                var existing = await _context.DbContext.CourierVehicles
                    .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.CourierId == courierId);
                if (existing == null)
                {
                    result.AddError("Araç bulunamadı.");
                    return result;
                }
                existing.VehicleType = dto.VehicleType;
                existing.LicensePlate = dto.LicensePlate.Trim();
                existing.DriverName = string.IsNullOrWhiteSpace(driverName) ? null : driverName;
                existing.DriverPhone = string.IsNullOrWhiteSpace(driverPhone) ? null : driverPhone;
                existing.DriverUserId = driverUserId;
                await _context.DbContext.SaveChangesAsync();
                result.Result = new CourierVehicleListDto
                {
                    Id = existing.Id,
                    CourierId = existing.CourierId,
                    VehicleType = existing.VehicleType,
                    VehicleTypeName = existing.VehicleType.GetDisplayName(),
                    LicensePlate = existing.LicensePlate,
                    DriverName = existing.DriverName,
                    DriverPhone = existing.DriverPhone,
                    DriverUserId = existing.DriverUserId
                };
            }
            else
            {
                var vehicle = new CourierVehicle
                {
                    CourierId = courierId,
                    VehicleType = dto.VehicleType,
                    LicensePlate = dto.LicensePlate.Trim(),
                    DriverName = string.IsNullOrWhiteSpace(driverName) ? null : driverName,
                    DriverPhone = string.IsNullOrWhiteSpace(driverPhone) ? null : driverPhone,
                    DriverUserId = driverUserId
                };
                _context.DbContext.CourierVehicles.Add(vehicle);
                await _context.DbContext.SaveChangesAsync();
                result.Result = new CourierVehicleListDto
                {
                    Id = vehicle.Id,
                    CourierId = vehicle.CourierId,
                    VehicleType = vehicle.VehicleType,
                    VehicleTypeName = vehicle.VehicleType.GetDisplayName(),
                    LicensePlate = vehicle.LicensePlate,
                    DriverName = vehicle.DriverName,
                    DriverPhone = vehicle.DriverPhone,
                    DriverUserId = vehicle.DriverUserId
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier SaveVehicle Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<Empty>> DeleteVehicle(int courierId, int vehicleId)
    {
        var result = new IActionResult<Empty>();
        try
        {
            var vehicle = await _context.DbContext.CourierVehicles
                .FirstOrDefaultAsync(x => x.Id == vehicleId && x.CourierId == courierId);
            if (vehicle == null)
            {
                result.AddError("Araç bulunamadı.");
                return result;
            }
            // Bu araca bağlı hizmet bölgelerindeki araç referansını kaldır (araç silinsin diye)
            var areasWithVehicle = await _context.DbContext.CourierServiceAreas
                .Where(s => s.CourierVehicleId == vehicleId && s.CourierId == courierId)
                .ToListAsync();
            foreach (var area in areasWithVehicle)
                area.CourierVehicleId = null;
            _context.DbContext.CourierVehicles.Remove(vehicle);
            await _context.DbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier DeleteVehicle Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<List<CourierServiceAreaListDto>>> GetServiceAreas(int courierId)
    {
        var result = new IActionResult<List<CourierServiceAreaListDto>> { Result = new List<CourierServiceAreaListDto>() };
        try
        {
            var list = await _context.DbContext.CourierServiceAreas
                .AsNoTracking()
                .Include(x => x.City)
                .Include(x => x.Town)
                .Include(x => x.Neighboor)
                .Include(x => x.CourierVehicle).ThenInclude(v => v!.DriverUser)
                .Include(x => x.Courier!).ThenInclude(c => c.ApplicationUser)
                .Where(x => x.CourierId == courierId)
                .OrderBy(x => x.City!.Name).ThenBy(x => x.Town!.Name)
                .ToListAsync();
            result.Result = list.Select(s => new CourierServiceAreaListDto
            {
                Id = s.Id,
                CourierId = s.CourierId,
                CourierName = GetServiceAreaDisplayName(s),
                CourierVehicleId = s.CourierVehicleId,
                VehicleDisplay = s.CourierVehicle != null ? $"{s.CourierVehicle.VehicleType.GetDisplayName()} - {s.CourierVehicle.LicensePlate}" : null,
                CityId = s.CityId,
                CityName = s.City?.Name ?? "",
                TownId = s.TownId,
                TownName = s.Town?.Name ?? "",
                NeighboorId = s.NeighboorId,
                NeighboorName = s.Neighboor?.Name,
                WorkStartTime = s.WorkStartTime?.ToString("HH:mm"),
                WorkEndTime = s.WorkEndTime?.ToString("HH:mm"),
                IsActive = s.IsActive
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier GetServiceAreas Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    /// <summary>Hizmet bölgesi satırında gösterilecek isim: araçta şoför (alt kurye) varsa onun adı, yoksa ana kurye adı.</summary>
    private static string? GetServiceAreaDisplayName(CourierServiceArea s)
    {
        if (s.CourierVehicle?.DriverUser != null)
        {
            var u = s.CourierVehicle.DriverUser;
            var name = string.IsNullOrWhiteSpace(u.FullName) ? $"{u.FirstName} {u.LastName}".Trim() : u.FullName.Trim();
            if (!string.IsNullOrEmpty(name)) return name;
        }
        if (s.CourierVehicle != null && !string.IsNullOrWhiteSpace(s.CourierVehicle.DriverName))
            return s.CourierVehicle.DriverName.Trim();
        if (s.Courier?.ApplicationUser != null)
        {
            var u = s.Courier.ApplicationUser;
            var name = string.IsNullOrWhiteSpace(u.FullName) ? $"{u.FirstName} {u.LastName}".Trim() : u.FullName.Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }
        return null;
    }

    public async Task<IActionResult<Empty>> SaveServiceAreas(int courierId, List<CourierServiceAreaUpsertDto> areas)
    {
        var result = new IActionResult<Empty>();
        try
        {
            var existing = await _context.DbContext.CourierServiceAreas.Where(x => x.CourierId == courierId).ToListAsync();
            _context.DbContext.CourierServiceAreas.RemoveRange(existing);

            foreach (var a in areas ?? new List<CourierServiceAreaUpsertDto>())
            {
                TimeOnly? start = null;
                TimeOnly? end = null;
                if (TimeOnly.TryParse(a.WorkStartTime, out var startParsed))
                    start = startParsed;
                if (TimeOnly.TryParse(a.WorkEndTime, out var endParsed))
                    end = endParsed;
                _context.DbContext.CourierServiceAreas.Add(new CourierServiceArea
                {
                    CourierId = courierId,
                    CourierVehicleId = a.CourierVehicleId > 0 ? a.CourierVehicleId : null,
                    CityId = a.CityId,
                    TownId = a.TownId,
                    NeighboorId = a.NeighboorId,
                    WorkStartTime = start,
                    WorkEndTime = end,
                    IsActive = a.IsActive
                });
            }
            await _context.DbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Courier SaveServiceAreas Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }
}
