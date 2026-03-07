using AutoMapper;
using ecommerce.Admin.Domain.Dtos.PaymentTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Npgsql;
namespace ecommerce.Admin.Domain.Concreate;

public class PaymentTypeService : IPaymentTypeService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<ecommerce.Core.Entities.Accounting.PaymentType> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentTypeService> _logger;
    private readonly IRadzenPagerService<PaymentTypeListDto> _radzenPagerService;
    private readonly ITenantProvider _tenantProvider;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "payment-types";

    public PaymentTypeService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger<PaymentTypeService> logger,
        IRadzenPagerService<PaymentTypeListDto> radzenPagerService,
        ITenantProvider tenantProvider,
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory,
        ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _context = context;
        _repository = context.GetRepository<ecommerce.Core.Entities.Accounting.PaymentType>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
        _roleFilter = roleFilter;
        _permissionService = permissionService;
    }

    private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
    private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
    private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
    private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

    public async Task<IActionResult<Paging<IQueryable<PaymentTypeListDto>>>> GetPaymentTypes(PageSetting pager)
    {
        var response = new IActionResult<Paging<IQueryable<PaymentTypeListDto>>> { Result = new() };
        try
        {
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            var query = _repository.GetAll(ignoreQueryFilters: true)
                .Where(x => x.IsActive); // Assuming we want active ones, or maybe all depending on req. Let's stick to base logic but use filter.
                                         // Original code didn't filter by IsActive in GetPaymentTypes, but did in GetAllPaymentTypes. 
                                         // Let's check original: "var entities = await _repository.GetAllAsync..."
            
            // Re-reading original: It was manually checking AllowBranchIds.
            // We should use _roleFilter.
            
            IQueryable<ecommerce.Core.Entities.Accounting.PaymentType> q = _repository.GetAll(ignoreQueryFilters: true)
                .Include(x => x.Currency);
            q = _roleFilter.ApplyFilter(q, _context.DbContext);
            
            var entities = await q.ToListAsync(); // ToPagedResultAsync/MakeDataQueryable handles paging usually, but here it returns IQueryable content.
                                                  // The original implementation fetched all then mapped then paged.
            
            // Let's stick to original flow but replacing manual security logic.
             var entitiesList = await q.ToListAsync();

            var mapped = _mapper.Map<List<PaymentTypeListDto>>(entitiesList);
            response.Result.Data = mapped.AsQueryable().OrderByDescending(x => x.Id);
            response.Result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPaymentTypes error");
            response.AddSystemError(ex.Message);
            return response;
        }
    }

    public async Task<IActionResult<List<PaymentTypeListDto>>> GetAllPaymentTypes()
    {
        var response = new IActionResult<List<PaymentTypeListDto>> { Result = new() };
        try
        {
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            IQueryable<ecommerce.Core.Entities.Accounting.PaymentType> query = _repository.GetAll(ignoreQueryFilters: true)
                .Where(x => x.IsActive);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            var items = await query.ToListAsync();
            response.Result = _mapper.Map<List<PaymentTypeListDto>>(items);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllPaymentTypes error");
            response.AddSystemError(ex.Message);
            return response;
        }
    }

    /// <summary>
    /// Ödeme tipi listesini yetki kontrolü olmadan, sadece BranchId filtreleme ile döndürür.
    /// Fatura oluşturma modal'ından çağrılır.
    /// </summary>
    public async Task<IActionResult<List<PaymentTypeListDto>>> GetAllPaymentTypesForInvoice()
    {
        var response = new IActionResult<List<PaymentTypeListDto>> { Result = new() };
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var repo = ctx.GetRepository<ecommerce.Core.Entities.Accounting.PaymentType>();

            var query = repo.GetAll(disableTracking: true, ignoreQueryFilters: true)
                .Where(x => x.IsActive);

            // Global query filter ve ApplyFilter bypass — doğrudan BranchId filtreleme
            var currentBranchId = _tenantProvider.GetCurrentBranchId();
            if (currentBranchId > 0)
            {
                // Belirli bir şube seçiliyse: sadece o şubeye ait + global (BranchId=0) olanları getir
                query = query.Where(x => x.BranchId == currentBranchId || x.BranchId == 0);
            }
            else if (!_tenantProvider.IsGlobalAdmin)
            {
                // BranchId=0 (Merkez Ofis) ve Global Admin değil → UserBranches'a göre filtrele
                var allowedBranchIds = await _roleFilter.GetAllowedBranchIdsAsync(ctx.DbContext);
                if (allowedBranchIds.Any())
                {
                    query = query.Where(x => allowedBranchIds.Contains(x.BranchId) || x.BranchId == 0);
                }
                else
                {
                    // Hiç branch erişimi yok → sadece global kayıtlar
                    query = query.Where(x => x.BranchId == 0);
                }
            }
            // Global Admin + BranchId=0 → tüm kayıtlar

            var items = await query.ToListAsync();
            response.Result = _mapper.Map<List<PaymentTypeListDto>>(items);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllPaymentTypesForInvoice error");
            response.AddSystemError(ex.Message);
            return response;
        }
    }

    public async Task<IActionResult<PaymentTypeUpsertDto>> GetPaymentTypeById(int id)
    {
        var response = new IActionResult<PaymentTypeUpsertDto> { Result = new() };
        try
        {
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            IQueryable<ecommerce.Core.Entities.Accounting.PaymentType> query = _repository.GetAll(ignoreQueryFilters: true)
                .Include(x => x.Currency)
                .Where(x => x.Id == id);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);

            var entity = await query.FirstOrDefaultAsync();

            if (entity == null)
            {
                 var exists = await _repository.GetAll(ignoreQueryFilters: true).AnyAsync(x => x.Id == id);
                 if (exists)
                 {
                      response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                 }
                 return response;
            }

            if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
            {
                 response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                 return response;
            }
            
            response.Result = _mapper.Map<PaymentTypeUpsertDto>(entity);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPaymentTypeById error");
            response.AddSystemError(ex.Message);
            return response;
        }
    }

    public async Task<IActionResult<Empty>> UpsertPaymentType(AuditWrapDto<PaymentTypeUpsertDto> wrap)
    {
        var response = new IActionResult<Empty> { Result = new() };
        try
        {
            var dto = wrap.Dto;
            if (dto.Id == 0)
            {
                if (!await CanCreate())
                {
                    response.AddError("Ekleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                // Check for duplicate name in current branch
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(pt => pt.Name.ToLower() == dto.Name.ToLower() && pt.BranchId == currentBranchId && pt.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    response.AddError($"'{dto.Name}' isimli ödeme tipi bu şubede zaten mevcut.");
                    return response;
                }

                var entity = _mapper.Map<ecommerce.Core.Entities.Accounting.PaymentType>(dto);
                entity.BranchId = currentBranchId;
                entity.CreatedId = wrap.UserId;
                entity.CreatedDate = DateTime.Now;
                await _repository.InsertAsync(entity);
            }
            else
            {
                if (!await CanEdit())
                {
                    response.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(ignoreQueryFilters: true)
                    .Where(x => x.Id == dto.Id);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                    response.AddError("Ödeme tipi bulunamadı veya yetkiniz yok.");
                    return response;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                     response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return response;
                }

                // Check for duplicate name in same branch (excluding current entity)
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(pt => pt.Id != dto.Id && pt.Name.ToLower() == dto.Name.ToLower() && pt.BranchId == entity.BranchId && pt.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    response.AddError($"'{dto.Name}' isimli ödeme tipi bu şubede zaten mevcut.");
                    return response;
                }

                _mapper.Map(dto, entity);
                entity.ModifiedId = wrap.UserId;
                entity.ModifiedDate = DateTime.Now;
                _repository.Update(entity);
            }

            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                response.AddSuccess("Başarılı");
                return response;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null)
                {
                    if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        response.AddError($"'{dto.Name}' isimli ödeme tipi zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                    }
                    else
                    {
                        response.AddError("Bir hata oluştu. Lütfen tekrar deneyiniz.");
                        _logger.LogError(lastResult.Exception, "UpsertPaymentType save error");
                    }
                }
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpsertPaymentType error");
            if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                response.AddError($"'{wrap.Dto.Name}' isimli ödeme tipi zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
            }
            else
            {
                response.AddSystemError(ex.Message);
            }
            return response;
        }
    }

    public async Task<IActionResult<Empty>> DeletePaymentType(AuditWrapDto<PaymentTypeDeleteDto> wrap)
    {
        var response = new IActionResult<Empty> { Result = new() };
        try
        {
            if (!await CanDelete())
            {
                response.AddError("Silme yetkiniz bulunmamaktadır.");
                return response;
            }

            var query = _repository.GetAll(ignoreQueryFilters: true)
                .Where(x => x.Id == wrap.Dto.Id);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);

            var entity = await query.FirstOrDefaultAsync();

            if (entity != null)
            {
                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                     response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return response;
                }

                _repository.Delete(entity);
                await _context.SaveChangesAsync();
                response.AddSuccess("Başarılı");
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeletePaymentType error");
            response.AddSystemError(ex.Message);
            return response;
        }
    }
}
