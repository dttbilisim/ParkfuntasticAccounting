using AutoMapper;
using ecommerce.Admin.Domain.Dtos.WarehouseDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities.Warehouse;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Interfaces;
using Npgsql;

namespace ecommerce.Admin.Domain.Concreate
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Warehouse> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<WarehouseService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "warehouses";

        public WarehouseService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger<WarehouseService> logger, IServiceScopeFactory serviceScopeFactory, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<Warehouse>();
            _mapper = mapper;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Paging<List<WarehouseListDto>>>> GetWarehouses(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<WarehouseListDto>>>();

            try
            {
                // Aynı request scope'taki DbContext/TenantProvider kullanılmalı; yeni scope açılırsa
                // TenantProvider (GetCurrentBranchId) farklı olur ve kaydedilen depo listede görünmez.
                var ctx = _context.DbContext;
                var query = ctx.Warehouses
                    .Include(s => s.City)
                    .Include(s => s.Town)
                    .Where(s => s.Status != (int)EntityStatus.Deleted)
                    .AsQueryable();

                // Role-based filtering - clean ve maintainable
                query = _roleFilter.ApplyFilter(query, ctx);

                response.Result = await query.ToPagedResultAsync<WarehouseListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetWarehouses Exception " + e);
                response.AddSystemError(e.Message);
            }

            return response;
        }

        public async Task<IActionResult<List<WarehouseListDto>>> GetAllWarehouses()
        {
             IActionResult<List<WarehouseListDto>> response = new() { Result = new() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<Warehouse>();
                    
                    var query = repository.GetAll(disableTracking: true, ignoreQueryFilters: true)
                        .Include(s => s.City)
                        .Include(s => s.Town)
                        .Where(f => f.Status == (int)EntityStatus.Active);

                    // Role-based filtering - clean ve maintainable
                    query = _roleFilter.ApplyFilter(query, context.DbContext);

                    var warehouses = await query.ToListAsync();
                    response.Result = _mapper.Map<List<WarehouseListDto>>(warehouses);
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllWarehouses Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertWarehouse(AuditWrapDto<WarehouseUpsertDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                
                if (!dto.Id.HasValue || dto.Id == 0)
                {
                     if (!await _permissionService.CanCreate(MENU_NAME))
                    {
                        rs.AddError("Ekleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }

                    // Check for duplicate name in current branch
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(w => w.Name.ToLower() == dto.Name.ToLower() && w.BranchId == currentBranchId && w.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli depo bu şubede zaten mevcut.");
                        return rs;
                    }

                    var entity = _mapper.Map<Warehouse>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;

                    if (entity.BranchId == 0) entity.BranchId = currentBranchId;

                    await _repository.InsertAsync(entity);
                    rs.AddSuccess("Kayıt başarıyla eklendi.");
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true,
                        ignoreQueryFilters: true);

                    if (!await _permissionService.CanEdit(MENU_NAME))
                    {
                        rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }

                    if (current == null)
                    {
                        rs.AddError("Depo bulunamadı");
                        return rs;
                    }

                    if (!await _roleFilter.CanAccessBranchAsync(current.BranchId, _context.DbContext))
                    {
                        rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return rs;
                    }

                    // Check for duplicate name in same branch (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(w => w.Id != dto.Id && w.Name.ToLower() == dto.Name.ToLower() && w.BranchId == current.BranchId && w.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli depo bu şubede zaten mevcut.");
                        return rs;
                    }

                    var updated = _mapper.Map<Warehouse>(dto);
                    updated.Id = current.Id;
                    updated.BranchId = current.BranchId; // Preserve branch
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                    rs.AddSuccess("Kayıt başarıyla güncellendi.");
                }

                await _context.SaveChangesAsync();
                var result = _context.LastSaveChangesResult;
                if (result.IsOk)
                {
                    rs.AddSuccess("Kayıt İşlemi Başarılı");
                    return rs;
                }
                else
                {
                    if (result.Exception != null)
                    {
                        if (result.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            rs.AddError($"'{dto.Name}' isimli depo zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                        }
                        else
                        {
                            rs.AddError("Bir hata oluştu. Lütfen tekrar deneyiniz.");
                        }
                    }
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertWarehouse Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    rs.AddError($"'{model.Dto.Name}' isimli depo zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                }
                else
                {
                    rs.AddSystemError(ex.ToString());
                }
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteWarehouse(AuditWrapDto<WarehouseDeleteDto> model)
        {
             var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await _permissionService.CanDelete(MENU_NAME))
                {
                    rs.AddError("Bu işlem için yetkiniz bulunmamaktadır.");
                    return rs;
                }

                // Check dependencies (Shelves)
                var shelfExists = await _context.DbContext.WarehouseShelves.AsNoTracking()
                    .AnyAsync(x => x.WarehouseId == model.Dto.Id && x.Status != (int)EntityStatus.Deleted);
                
                if (shelfExists)
                {
                    rs.AddError("Bu depoya bağlı raflar var. Önce rafları silmelisiniz.");
                    return rs;
                }

                await _context.DbContext.Warehouses
                    .IgnoreQueryFilters()
                    .Where(f => f.Id == model.Dto.Id && f.BranchId == _tenantProvider.GetCurrentBranchId())
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                        .SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                rs.AddSuccess("Başarıyla silindi");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteWarehouse Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<WarehouseUpsertDto>> GetWarehouseById(int Id)
        {
             var rs = new IActionResult<WarehouseUpsertDto> { Result = new WarehouseUpsertDto() };
            try
            {
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();

                var entity = await _repository.GetFirstOrDefaultAsync(
                    predicate: f => f.Id == Id 
                        && (isGlobalAdmin ? (currentBranchId == 0 || f.BranchId == currentBranchId) : true), 
                    ignoreQueryFilters: true);
                
                if (entity != null && !isGlobalAdmin)
                {
                    var user = _httpContextAccessor.HttpContext?.User;
                    var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         var isAllowed = await _context.DbContext.UserBranches
                            .AnyAsync(ub => ub.UserId == userId && ub.BranchId == entity.BranchId && ub.Status == (int)EntityStatus.Active);
                         if (!isAllowed)
                         {
                             rs.AddError("Bu depoyu görme yetkiniz yok.");
                             return rs;
                         }
                         
                         if (currentBranchId > 0 && entity.BranchId != currentBranchId)
                         {
                             rs.AddError("Depo seçili şubeye ait değil.");
                             return rs;
                         }
                    }
                }

                var dto = _mapper.Map<WarehouseUpsertDto>(entity);
                if (dto != null)
                {
                    dto.StatusBool = dto.Status == (int)EntityStatus.Active;
                    rs.Result = dto;
                }
                else rs.AddError("Depo Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetWarehouseById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<List<WarehouseListDto>>> GetWarehousesByBranchId(int branchId)
        {
            var response = new IActionResult<List<WarehouseListDto>>() { Result = new List<WarehouseListDto>() };
            try
            {
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;
                int userId = 0;
                if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                if (!isGlobalAdmin)
                {
                    var isAllowed = await _context.DbContext.UserBranches
                        .AnyAsync(ub => ub.UserId == userId && ub.BranchId == branchId && ub.Status == (int)EntityStatus.Active);
                    if (!isAllowed)
                    {
                        response.AddError("Bu şubeye ait depoları görme yetkiniz yok.");
                        return response;
                    }
                }

                var warehouses = await _repository.GetAll(disableTracking: true, ignoreQueryFilters: true)
                    .Where(w => w.BranchId == branchId && w.Status == (int)EntityStatus.Active)
                    .Include(s => s.City)
                    .Include(s => s.Town)
                    .ToListAsync();

                response.Result = _mapper.Map<List<WarehouseListDto>>(warehouses);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetWarehousesByBranchId Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}


