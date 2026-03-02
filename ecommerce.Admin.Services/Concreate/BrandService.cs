using AutoMapper;
using ecommerce.Admin.Domain.Dtos.BrandDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Npgsql;
namespace ecommerce.Admin.Domain.Concreate
{
    public class BrandService : IBrandService
    {
        private const string MENU_NAME = "brands";
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Brand> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<BrandListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;

        public BrandService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<BrandListDto> radzenPagerService, IServiceScopeFactory serviceScopeFactory, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<Brand>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }
        public async Task<IActionResult<Paging<List<BrandListDto>>>> GetBrands(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<BrandListDto>>>();

            try
            {
                var query = _repository.GetAll(ignoreQueryFilters: true)
                    .Where(s => s.Status != (int)EntityStatus.Deleted);

                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                response.Result = await query.ToPagedResultAsync<BrandListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetBrands Exception " + e);
                response.AddSystemError(e.Message);
            }

            return response;
        }

        public async Task<IActionResult<Empty>> DeleteBrand(AuditWrapDto<BrandDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                if (!await _permissionService.CanDelete(MENU_NAME))
                {
                    rs.AddError("Bu işlem için yetkiniz bulunmamaktadır.");
                    return rs;
                }

                var productControl = await _context.DbContext.Product.AsNoTracking().Where(x => x.BrandId == model.Dto.Id && x.Status == (int)EntityStatus.Active).AnyAsync();
                if (productControl)
                {
                    rs.AddError("Bu markayı silemezsiniz. Bu markaya ait ürünler mevcut");
                    return rs;
                }

                //Deleted Mark with audit
                await _context.DbContext.Brand
                    .Where(f => f.Id == model.Dto.Id && f.BranchId == _tenantProvider.GetCurrentBranchId())
                    .ExecuteUpdateAsync(x => x
                    .SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                    .SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));



                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Successfull");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteBrand Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<BrandUpsertDto>> GetBrandById(int Id)
        {
            var rs = new IActionResult<BrandUpsertDto>
            {
                Result = new BrandUpsertDto()
            };
            try
            {
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();

                var brand = await _repository.GetFirstOrDefaultAsync(
                    predicate: f => f.Id == Id 
                        && (isGlobalAdmin ? (currentBranchId == 0 || f.BranchId == currentBranchId) : true),
                    ignoreQueryFilters: true);

                if (brand != null && !isGlobalAdmin)
                {
                    var user = _httpContextAccessor.HttpContext?.User;
                    var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         var isAllowed = await _context.DbContext.UserBranches
                            .AnyAsync(ub => ub.UserId == userId && ub.BranchId == brand.BranchId && ub.Status == (int)EntityStatus.Active);
                         if (!isAllowed)
                         {
                             rs.AddError("Bu markayı görme yetkiniz yok.");
                             return rs;
                         }
                         
                         if (currentBranchId > 0 && brand.BranchId != currentBranchId)
                         {
                             rs.AddError("Marka seçili şubeye ait değil.");
                             return rs;
                         }
                    }
                }

                var mappedCat = _mapper.Map<BrandUpsertDto>(brand);
                if (mappedCat != null)
                {
                    rs.Result = mappedCat;
                }
                else rs.AddError("Marka Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBrand Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertBrand(AuditWrapDto<BrandUpsertDto> model)
        {
            


            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                
                if (!dto.Id.HasValue)
                {
                     if (!await _permissionService.CanCreate(MENU_NAME))
                    {
                        rs.AddError("Ekleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }

                    // Check for duplicate name in current branch
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(b => b.Name.ToLower() == dto.Name.ToLower() && b.BranchId == currentBranchId && b.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli marka bu şubede zaten mevcut.");
                        return rs;
                    }

                    var entity = _mapper.Map<Brand>(dto);
                    entity.BranchId = currentBranchId;
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                    rs.AddSuccess("insert");
                }
                else
                {
                    if (!await _permissionService.CanEdit(MENU_NAME))
                    {
                        rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if (current == null)
                    {
                        rs.AddError("Marka bulunamadı");
                        return rs;
                    }
                    
                    if (!await _roleFilter.CanAccessBranchAsync(current.BranchId ?? 0, _context.DbContext))
                    {
                        rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return rs;
                    }

                     // Check for duplicate name in same branch (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(b => b.Id != dto.Id && b.Name.ToLower() == dto.Name.ToLower() && b.BranchId == current.BranchId && b.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli marka bu şubede zaten mevcut.");
                        return rs;
                    }

                    var updated = _mapper.Map<Brand>(dto);
                    updated.Id = current.Id;
                    updated.BranchId = current.BranchId; // Preserve branch
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                    rs.AddSuccess("update");
                }
                
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                   
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                    {
                        if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            rs.AddError($"'{dto.Name}' isimli marka zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                        }
                        else
                        {
                            rs.AddError(lastResult.Exception.ToString());
                        }
                    }
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBrand Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    rs.AddError($"'{model.Dto.Name}' isimli marka zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                }
                else
                {
                    rs.AddSystemError(ex.ToString());
                }
                return rs;
            }
        }

        public async Task<IActionResult<List<BrandListDto>>> GetBrands()
        {
            IActionResult<List<BrandListDto>> response = new() { Result = new() };
            try
            {
                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<Brand>();
                    
                    var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    var user = _httpContextAccessor.HttpContext?.User;
                    int userId = 0;
                    if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                    List<int> allowedBranchIds = new();
                    if (!isGlobalAdmin && userId > 0)
                    {
                            allowedBranchIds = context.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();
                    }

                    var brands = await repository.GetAllAsync(
                        predicate: f => f.Status == (int)EntityStatus.Active
                            && (
                                    isGlobalAdmin ? (currentBranchId == 0 || f.BranchId == currentBranchId) :
                                    (allowedBranchIds.Contains(f.BranchId ?? 0) && (currentBranchId == 0 || f.BranchId == currentBranchId))
                            ), 
                        disableTracking: true,
                        ignoreQueryFilters: true);
                    var mappedCats = _mapper.Map<List<BrandListDto>>(brands);
                    if (mappedCats != null)
                    {
                        if (mappedCats.Count > 0)
                            response.Result = mappedCats.ToList();
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBrands Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}


