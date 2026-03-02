using AutoMapper;
using ecommerce.Admin.Domain.Dtos.TierDto;
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
namespace ecommerce.Admin.Domain.Concreate
{
    public class TierService : ITierService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Tier> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<TierListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "tiers";

        public TierService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<TierListDto> radzenPagerService, IServiceScopeFactory serviceScopeFactory, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<Tier>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Empty>> DeleteTier(AuditWrapDto<TierDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                if (await _context.DbContext.ProductTiers.AnyAsync(x => x.TierId == model.Dto.Id))
                {
                    response.AddError("Bu ürün grubunu silemezsiniz. Bu ürün grubu diğer ürünlerde kullanılmaktadır.");
                    return response;
                }

                await _context.DbContext.Tiers.Where(f => f.Id == model.Dto.Id)
      .ExecuteUpdateAsync(x => x.SetProperty(x => x.DeletedId, model.UserId)
      .SetProperty(x => x.Status, EntityStatus.Deleted.GetHashCode()));


                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteTier Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<TierListDto>>>> GetTiers(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<TierListDto>>> response = new() { Result = new() };
            try
            {

                // Security Logic
                var currentBranchId = _tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : 0;
                var user = _httpContextAccessor.HttpContext?.User;
                var isGlobalAdmin = user != null && (user.IsInRole("SuperAdmin") || user.IsInRole("MainRoot") || user.IsInRole("Admin")); 

                List<int> allowedBranchIds = new();
                if (!isGlobalAdmin && user != null)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         allowedBranchIds = _context.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();
                    }
                }

                var categories = await _repository.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active
                             && (
                                 isGlobalAdmin ? (currentBranchId == 0 || x.BranchId == currentBranchId) :
                                 (allowedBranchIds.Contains(x.BranchId ?? 0) && (currentBranchId == 0 || x.BranchId == currentBranchId))
                            ));
                var mappedEntities = _mapper.Map<List<TierListDto>>(categories);
                if (mappedEntities != null)
                {
                    if (mappedEntities.Count > 0)
                    {
                        response.Result.Data = mappedEntities.AsQueryable();
                    }
                }

                if (response.Result.Data != null)
                    response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);


                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);


                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTiers Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<TierListDto>>> GetTiers()
        {
            var response = new IActionResult<List<TierListDto>>
            {
                Result = new List<TierListDto>()
            };
            try
            {
                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<Tier>();
                    
                    var currentBranchId = _tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : 0;
                    var categories = await repository.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active && (currentBranchId == 0 || x.BranchId == currentBranchId), disableTracking: true);
                    var mappedEntites = _mapper.Map<List<TierListDto>>(categories);
                    if (mappedEntites != null)
                    {
                        if (mappedEntites.Count > 0)
                            response.Result = mappedEntites;
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTiers Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<TierUpsertDto>> GetTiersById(int categoryId)
        {
            var response = new IActionResult<TierUpsertDto>
            {
                Result = new()
            };
            try
            {
                var categories = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == categoryId);
                var mappedEntity = _mapper.Map<TierUpsertDto>(categories);
                if (mappedEntity != null)
                {
                    response.Result = mappedEntity;
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCategoriesForParentSelectList Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertTier(AuditWrapDto<TierUpsertDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    // Check for duplicate name in current branch
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(t => t.Name.ToLower() == dto.Name.ToLower() && t.BranchId == currentBranchId && t.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli ürün grubu bu şubede zaten mevcut.");
                        return response;
                    }

                    var entity = _mapper.Map<Tier>(dto);
                    entity.BranchId = currentBranchId;
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if (current == null)
                    {
                        response.AddError("Ürün grubu bulunamadı");
                        return response;
                    }

                    // Check for duplicate name in same branch (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(t => t.Id != dto.Id && t.Name.ToLower() == dto.Name.ToLower() && t.BranchId == current.BranchId && t.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli ürün grubu bu şubede zaten mevcut.");
                        return response;
                    }

                    var updated = _mapper.Map<Tier>(dto);
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
                        if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            response.AddError($"'{dto.Name}' isimli ürün grubu zaten mevcut.");
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
                _logger.LogError("UpsertTier Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    response.AddError($"'{model.Dto.Name}' isimli ürün grubu zaten mevcut.");
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
