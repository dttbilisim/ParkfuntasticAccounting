using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductTypeDto;
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
    public class ProductTypeService : IProductTypeService
    {
        private const string MENU_NAME = "product-types";
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<ProductType> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<ProductTypeListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;

        public ProductTypeService(
            IUnitOfWork<ApplicationDbContext> context, 
            IMapper mapper, 
            ILogger logger, 
            IRadzenPagerService<ProductTypeListDto> radzenPagerService, 
            IServiceScopeFactory serviceScopeFactory, 
            ITenantProvider tenantProvider, 
            IHttpContextAccessor httpContextAccessor,
            ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<ProductType>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }

        private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
        private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
        private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
        private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

        public async Task<IActionResult<Empty>> DeleteProductType(AuditWrapDto<ProductTypeDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                if (!await CanDelete())
                {
                    response.AddError("Silme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _context.DbContext.ProductType
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == model.Dto.Id);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var current = await query.FirstOrDefaultAsync();

                if (current == null)
                {
                     response.AddError("Ürün tipi bulunamadı veya yetkiniz yok.");
                     return response;
                }

                if (!await _roleFilter.CanAccessBranchAsync(current.BranchId ?? 0, _context.DbContext))
                {
                     response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return response;
                }

                await _context.DbContext.ProductType
                    .Where(x => x.Id == model.Dto.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(product => product.DeletedId, model.UserId)
                        .SetProperty(productType => productType.Status, EntityStatus.Deleted.GetHashCode()));


                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }

                if (lastResult != null && lastResult.Exception != null)
                    response.AddError(lastResult.Exception.ToString());

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteProductType Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<ProductTypeUpsertDto>> GetProductTypeById(int Id)
        {
            IActionResult<ProductTypeUpsertDto> response = new IActionResult<ProductTypeUpsertDto> { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: f => f.Id == Id,
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var product = await query.FirstOrDefaultAsync();

                if (product == null)
                {
                     // Check existance for security msg
                    var exists = await _repository.GetAll(predicate: x => x.Id == Id, ignoreQueryFilters: true).AnyAsync();
                    if (exists)
                    {
                         response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return response;
                    }
                    response.AddError("Ürün Tipi Bulunamadı");
                    return response;
                }

                if (!await _roleFilter.CanAccessBranchAsync(product.BranchId ?? 0, _context.DbContext))
                {
                        response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return response;
                }

                var mappedCat = _mapper.Map<ProductTypeUpsertDto>(product);
                if (mappedCat != null)
                {
                    response.Result = mappedCat;
                }
                else response.AddError("Ürün Tipi Bulunamadı");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductType Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<ProductTypeListDto>>>> GetProductTypes(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<ProductTypeListDto>>> response = new() { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: x => x.Status == (int)EntityStatus.Active,
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var products = await query.ToListAsync();
                var mappedCats = _mapper.Map<List<ProductTypeListDto>>(products);
                if (mappedCats != null)
                {
                    if (mappedCats.Count > 0)
                        response.Result.Data = mappedCats.AsQueryable();
                }

                if (response.Result.Data != null)
                    response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);

                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
       

                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductTypeList Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<ProductTypeListDto>>> GetProductTypes()
        {
            IActionResult<List<ProductTypeListDto>> response = new() { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<ProductType>();
                    // var roleFilter = scope.ServiceProvider.GetRequiredService<ecommerce.Admin.Domain.Services.IRoleBasedFilterService>();
                    var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                    var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

                    var isGlobalAdmin = tenantProvider.IsGlobalAdmin;
                    var currentBranchId = tenantProvider.GetCurrentBranchId();
                    
                    var query = repository.GetAll(
                        predicate: x => x.Status == (int)EntityStatus.Active, 
                        disableTracking: true,
                        ignoreQueryFilters: true);
                    
                    // Manual Role Based Filtering with Global Item Inheritance
                    if (!isGlobalAdmin)
                    {
                        var user = httpContextAccessor.HttpContext?.User;
                        int userId = 0;
                        if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                        if (userId > 0)
                        {
                             var allowedBranchIds = await context.DbContext.UserBranches
                                .AsNoTracking()
                                .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                                .Select(ub => ub.BranchId)
                                .ToListAsync();

                             query = query.Where(x => 
                                !x.BranchId.HasValue || 
                                x.BranchId == 0 || 
                                allowedBranchIds.Contains(x.BranchId.Value));
                        }
                    }

                    // Filter by Current Branch Context, BUT allow Global items (Null or 0)
                    if (currentBranchId > 0)
                    {
                         query = query.Where(x => 
                            !x.BranchId.HasValue || 
                            x.BranchId == 0 || 
                            x.BranchId == currentBranchId);
                    }
                    
                    var products = await query.ToListAsync();

                    var mappedCats = _mapper.Map<List<ProductTypeListDto>>(products);
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
                _logger.LogError("GetProductTypes Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertProductType(AuditWrapDto<ProductTypeUpsertDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                var dto = model.Dto;
                
                if (!dto.Id.HasValue)
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
                        response.AddError($"'{dto.Name}' isimli ürün tipi bu şubede zaten mevcut.");
                        return response;
                    }

                    var entity = _mapper.Map<ProductType>(dto);
                    entity.BranchId = currentBranchId;
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
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

                    var query = _repository.GetAll(
                         predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                         disableTracking: true,
                         ignoreQueryFilters: true);
                    
                    query = _roleFilter.ApplyFilter(query, _context.DbContext);
                    
                    var current = await query.FirstOrDefaultAsync();

                    if (current == null)
                    {
                        response.AddError("Ürün tipi bulunamadı veya yetkiniz yok.");
                        return response;
                    }

                    if (!await _roleFilter.CanAccessBranchAsync(current.BranchId ?? 0, _context.DbContext))
                    {
                         response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return response;
                    }

                    // Check for duplicate name in same branch (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(pt => pt.Id != dto.Id && pt.Name.ToLower() == dto.Name.ToLower() && pt.BranchId == current.BranchId && pt.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli ürün tipi bu şubede zaten mevcut.");
                        return response;
                    }

                    var updated = _mapper.Map<ProductType>(dto);
                    updated.Id = current.Id;
                    updated.BranchId = current.BranchId; // Preserve branch
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                }

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                    {
                        if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            response.AddError($"'{dto.Name}' isimli ürün tipi zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                        }
                        else
                        {
                            response.AddError(lastResult.Exception.ToString());
                        }
                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpsertProductType Exception {ex.ToString()}");
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    response.AddError($"'{model.Dto.Name}' isimli ürün tipi zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                }
                else
                {
                    response.AddSystemError(ex.ToString());
                }
                return response;
            }
        }
    }
}
