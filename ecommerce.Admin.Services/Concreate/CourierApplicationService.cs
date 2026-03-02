using ecommerce.Admin.Domain.Dtos.CourierApplicationDto;
using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Extensions;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate;

public class CourierApplicationService : ICourierApplicationService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<CourierApplicationService> _logger;
    private const string MENU_NAME = "courier-applications";

    public CourierApplicationService(
        IUnitOfWork<ApplicationDbContext> context,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<CourierApplicationService> logger)
    {
        _context = context;
        _contextFactory = contextFactory;
        _permissionService = permissionService;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<IActionResult<int>> Create(int applicationUserId, CourierApplicationUpsertDto dto)
    {
        var result = new IActionResult<int> { Result = 0 };
        try
        {
            var existing = await _context.DbContext.CourierApplications
                .AsNoTracking()
                .Where(x => x.ApplicationUserId == applicationUserId && x.Status == CourierApplicationStatus.Pending)
                .AnyAsync();
            if (existing)
            {
                result.AddError("Zaten bekleyen bir başvurunuz bulunmaktadır.");
                return result;
            }

            var entity = new CourierApplication
            {
                ApplicationUserId = applicationUserId,
                Phone = dto.Phone.Trim(),
                IdentityNumber = string.IsNullOrWhiteSpace(dto.IdentityNumber) ? null : dto.IdentityNumber.Trim(),
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
                Status = CourierApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };
            _context.DbContext.CourierApplications.Add(entity);
            await _context.DbContext.SaveChangesAsync();
            result.Result = entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierApplication Create Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<Paging<List<CourierApplicationListDto>>>> GetPaged(PageSetting pager, CourierApplicationStatus? status = null)
    {
        var result = new IActionResult<Paging<List<CourierApplicationListDto>>>
        {
            Result = new Paging<List<CourierApplicationListDto>> { Data = new List<CourierApplicationListDto>(), DataCount = 0 }
        };
        try
        {
            if (!await _permissionService.CanView(MENU_NAME))
            {
                result.AddError("Bu sayfayı görüntüleme yetkiniz bulunmamaktadır.");
                return result;
            }

            await using var db = await _contextFactory.CreateDbContextAsync();
            var query = db.CourierApplications
                .AsNoTracking()
                .Include(x => x.ApplicationUser)
                .Include(x => x.City)
                .Include(x => x.Town)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            var totalCount = await query.CountAsync();
            var skip = pager.Skip ?? 0;
            var take = Math.Min(pager.Take ?? 25, 500);

            var list = await query
                .OrderByDescending(x => x.AppliedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var dtos = list.Select(x => new CourierApplicationListDto
            {
                Id = x.Id,
                ApplicationUserId = x.ApplicationUserId,
                UserName = x.ApplicationUser?.FullName ?? "",
                Email = x.ApplicationUser?.Email,
                Phone = x.Phone,
                IdentityNumber = x.IdentityNumber,
                Note = x.Note,
                TaxNumber = x.TaxNumber,
                TaxOffice = x.TaxOffice,
                IBAN = x.IBAN,
                CityId = x.CityId,
                TownId = x.TownId,
                CityName = x.City?.Name,
                TownName = x.Town?.Name,
                TaxPlatePath = x.TaxPlatePath,
                SignatureDeclarationPath = x.SignatureDeclarationPath,
                IdCopyPath = x.IdCopyPath,
                CriminalRecordPath = x.CriminalRecordPath,
                Status = x.Status,
                StatusName = x.Status.GetDisplayName(),
                AppliedAt = x.AppliedAt,
                ReviewedAt = x.ReviewedAt,
                ReviewedByUserId = x.ReviewedByUserId,
                RejectReason = x.RejectReason
            }).ToList();

            result.Result = new Paging<List<CourierApplicationListDto>>
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
            _logger.LogError(ex, "CourierApplication GetPaged Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<Empty>> Review(int applicationId, CourierApplicationReviewDto dto, int reviewedByUserId)
    {
        var result = new IActionResult<Empty>();
        try
        {
            if (!await _permissionService.CanView(MENU_NAME))
            {
                result.AddError("Bu işlem için yetkiniz bulunmamaktadır.");
                return result;
            }

            var app = await _context.DbContext.CourierApplications
                .Include(x => x.ApplicationUser)
                .FirstOrDefaultAsync(x => x.Id == applicationId);
            if (app == null)
            {
                result.AddError("Başvuru bulunamadı.");
                return result;
            }
            if (app.Status != CourierApplicationStatus.Pending)
            {
                result.AddError("Bu başvuru zaten işlenmiş.");
                return result;
            }

            app.Status = dto.Approve ? CourierApplicationStatus.Approved : CourierApplicationStatus.Rejected;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewedByUserId = reviewedByUserId;
            app.RejectReason = dto.RejectReason;

            if (dto.Approve)
            {
                var existingCourier = await _context.DbContext.Couriers
                    .AsNoTracking()
                    .Where(x => x.ApplicationUserId == app.ApplicationUserId && x.Status == (int)EntityStatus.Active)
                    .AnyAsync();
                if (existingCourier)
                {
                    result.AddError("Bu kullanıcı zaten kurye olarak kayıtlı.");
                    return result;
                }
                var courier = new Courier
                {
                    ApplicationUserId = app.ApplicationUserId,
                    Status = (int)EntityStatus.Active,
                    CreatedDate = DateTime.UtcNow,
                    CreatedId = reviewedByUserId
                };
                _context.DbContext.Couriers.Add(courier);

                // "Courier" rolü yoksa oluştur, kullanıcıya ata
                const string courierRoleName = "Courier";
                if (!await _roleManager.RoleExistsAsync(courierRoleName))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = courierRoleName });
                }
                var appUser = await _userManager.FindByIdAsync(app.ApplicationUserId.ToString());
                if (appUser != null && !await _userManager.IsInRoleAsync(appUser, courierRoleName))
                {
                    await _userManager.AddToRoleAsync(appUser, courierRoleName);
                    // Başvuru sahibindeki CourierApplicant rolünü kaldır
                    if (await _userManager.IsInRoleAsync(appUser, "CourierApplicant"))
                        await _userManager.RemoveFromRoleAsync(appUser, "CourierApplicant");
                }
            }

            await _context.DbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierApplication Review Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }

    public async Task<IActionResult<Empty>> Delete(int applicationId)
    {
        var result = new IActionResult<Empty>();
        try
        {
            if (!await _permissionService.CanView(MENU_NAME))
            {
                result.AddError("Bu işlem için yetkiniz bulunmamaktadır.");
                return result;
            }

            var app = await _context.DbContext.CourierApplications
                .FirstOrDefaultAsync(x => x.Id == applicationId);
            if (app == null)
            {
                result.AddError("Başvuru bulunamadı.");
                return result;
            }

            _context.DbContext.CourierApplications.Remove(app);
            await _context.DbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CourierApplication Delete Exception");
            result.AddSystemError(ex.ToString());
        }
        return result;
    }
}
