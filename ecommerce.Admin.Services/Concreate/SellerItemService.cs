using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SellerItemDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using ecommerce.Admin.Domain.Services;

namespace ecommerce.Admin.Domain.Concreate
{
    public class SellerItemService : ISellerItemService
    {
        private const string MENU_NAME = "advertisements";
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<SellerItemService> _logger;
        private readonly IRoleBasedFilterService _roleFilter;
        private readonly IPermissionService _permissionService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SellerItemService(
            IServiceScopeFactory scopeFactory,
            IMapper mapper,
            ILogger<SellerItemService> logger,
            IRoleBasedFilterService roleFilter,
            IPermissionService permissionService,
            ITenantProvider tenantProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _scopeFactory = scopeFactory;
            _mapper = mapper;
            _logger = logger;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

        public async Task<IActionResult<Paging<List<SellerItemListDto>>>> GetSellerItems(PageSetting pager, int? sellerId = null)
        {
            var response = OperationResult.CreateResult<Paging<List<SellerItemListDto>>>();
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<SellerItem>();

                var query = repo.GetAll(true)
                    .IgnoreQueryFilters()
                    .Where(s => s.Status == (int)EntityStatus.Active);

                if (sellerId.HasValue)
                {
                    query = query.Where(x => x.SellerId == sellerId.Value);
                }

                // Apply multi-tenancy isolation via Seller's BranchId
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var isB2BAdmin = _tenantProvider.IsB2BAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();

                if (!isGlobalAdmin)
                {
                    if (isB2BAdmin)
                    {
                        if (currentBranchId > 0)
                        {
                            query = query.Where(x => x.Seller.BranchId == null || x.Seller.BranchId == currentBranchId);
                        }
                        else
                        {
                            // If currentBranchId is 0, filter by all allowed branch IDs for the user
                            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            if (int.TryParse(userIdClaim, out int userId))
                            {
                                var allowedBranchIds = uow.DbContext.UserBranches
                                    .AsNoTracking()
                                    .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                                    .Select(ub => ub.BranchId)
                                    .ToList();

                                if (allowedBranchIds.Any())
                                {
                                    query = query.Where(x => x.Seller.BranchId == null || allowedBranchIds.Contains(x.Seller.BranchId.Value));
                                }
                                else
                                {
                                    query = query.Where(x => x.Seller.BranchId == null);
                                }
                            }
                        }
                    }
                    else if (_tenantProvider.IsPlasiyer || _tenantProvider.IsCustomerB2B)
                    {
                        // Standard users see everything or follow other rules?
                        // Based on RoleBasedFilterService, they seem to see everything (no additional filter).
                    }
                    else
                    {
                        // Any other role without explicit access gets nothing
                        query = query.Where(x => false);
                    }
                }
                // Add includes
                query = query
                    .Include(s => s.Seller)
                    .Include(s => s.Product)
                        .ThenInclude(p => p.ProductGroupCodes);

                // Manual filtering for Seller (if added in search string or extra filter)
                // SearchFilterReguestDto is not passed here but PageSetting has Search property
                if (!string.IsNullOrEmpty(pager.Search))
                {
                    query = query.Where(x => 
                        x.Product.Name.Contains(pager.Search) || 
                        x.Product.Barcode.Contains(pager.Search) || 
                        x.Seller.Name.Contains(pager.Search) ||
                        x.Product.ProductGroupCodes.Any(pgc => pgc.OemCode.Contains(pager.Search)));
                }

                response.Result = await query.ToPagedResultAsync<SellerItemListDto>(pager, _mapper);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSellerItems Exception");
                response.AddSystemError(ex.Message);
            }
            return response;
        }

        public async Task<IActionResult<SellerItemUpsertDto>> GetSellerItemById(int id)
        {
            var response = new IActionResult<SellerItemUpsertDto>() { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<SellerItem>();

                var query = repo.GetAll(true)
                    .IgnoreQueryFilters()
                    .Include(x => x.Seller)
                    .Where(x => x.Id == id && x.Status == (int)EntityStatus.Active);

                var item = await query.FirstOrDefaultAsync();

                if (item == null)
                {
                    response.AddError("Kayıt bulunamadı.");
                    return response;
                }

                // Check access
                if (!CheckSellerAccess(item.Seller, uow.DbContext))
                {
                     response.AddError("Bu kaydı görüntüleme yetkiniz yok.");
                     return response;
                }

                response.Result = _mapper.Map<SellerItemUpsertDto>(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSellerItemById Exception");
                response.AddSystemError(ex.Message);
            }
            return response;
        }

        public async Task<IActionResult<SellerItem>> AddSellerItem(SellerItemUpsertDto model, int userId)
        {
            var response = new IActionResult<SellerItem>();
            try
            {
                if (!await _permissionService.CanCreate(MENU_NAME))
                {
                    response.AddError("Ekleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<SellerItem>();
                var sellerRepo = uow.GetRepository<Seller>();

                var seller = await sellerRepo.GetAll(true).IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == model.SellerId);
                if (seller == null)
                {
                    response.AddError("Seçilen satıcı bulunamadı.");
                    return response;
                }

                // Check access to seller
                if (!CheckSellerAccess(seller, uow.DbContext))
                {
                     response.AddError("Bu satıcı için işlem yapma yetkiniz yok.");
                     return response;
                }

                var entity = _mapper.Map<SellerItem>(model);
                entity.CreatedDate = DateTime.Now;
                entity.CreatedId = userId;
                entity.Status = (int)EntityStatus.Active;

                await repo.InsertAsync(entity);
                await uow.SaveChangesAsync();
                response.Result = entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddSellerItem Exception");
                response.AddSystemError(ex.Message);
            }
            return response;
        }

        public async Task<IActionResult<SellerItem>> UpdateSellerItem(SellerItemUpsertDto model, int userId)
        {
            var response = new IActionResult<SellerItem>();
            try
            {
                if (!await _permissionService.CanEdit(MENU_NAME))
                {
                    response.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<SellerItem>();

                var entity = await repo.GetAll(true)
                    .IgnoreQueryFilters()
                    .Include(x => x.Seller)
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                if (entity == null)
                {
                    response.AddError("Kayıt bulunamadı.");
                    return response;
                }
                
                // Check access to current entity
                if (!CheckSellerAccess(entity.Seller, uow.DbContext))
                {
                     response.AddError("Bu işlem için yetkiniz yok.");
                     return response;
                }

                // If seller changed, check access to new seller too
                if (entity.SellerId != model.SellerId)
                {
                    var sellerRepo = uow.GetRepository<Seller>();
                    var newSeller = await sellerRepo.GetAll(true).IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == model.SellerId);
                    if (newSeller == null || !CheckSellerAccess(newSeller, uow.DbContext))
                    {
                        response.AddError("Yeni seçilen satıcı için yetkiniz yok.");
                        return response;
                    }
                }

                _mapper.Map(model, entity);
                entity.ModifiedDate = DateTime.Now;
                entity.ModifiedId = userId;

                repo.Update(entity);
                await uow.SaveChangesAsync();
                response.Result = entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateSellerItem Exception");
                response.AddSystemError(ex.Message);
            }
            return response;
        }

        private bool CheckSellerAccess(Seller seller, ApplicationDbContext dbContext)
        {
            if (seller == null) return false;
            
            var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
            if (isGlobalAdmin) return true;

            var isB2BAdmin = _tenantProvider.IsB2BAdmin;
            if (isB2BAdmin)
            {
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                if (currentBranchId > 0)
                {
                    return seller.BranchId == null || seller.BranchId == currentBranchId;
                }
                else
                {
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         var allowedBranchIds = dbContext.UserBranches
                                    .AsNoTracking()
                                    .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                                    .Select(ub => ub.BranchId)
                                    .ToList();
                        
                        // User allowed branches contains seller branch or seller has no branch
                        if (allowedBranchIds.Any())
                        {
                            return seller.BranchId == null || allowedBranchIds.Contains(seller.BranchId.Value);
                        }
                        
                        // No allowed branches, only see global if logic permits, or nothing
                        return seller.BranchId == null;
                    }
                }
            }
            
            // Standard users (Plasiyer, Customer) - assuming they can create/edit if they have permission
            // but usually they shouldn't edit other people's stuff. 
            // For now, retaining similar logic to GetSellerItems (which seemed permissive for them or undefined)
            // But usually Plasiyer shouldn't create advertisements?
            // If they have permission via PermissionService, then we assume they can act.
            // But if we want to restrict Plasiyer to current branch...
            
            return true; // Default allow if permission service passed, subject to refinement
        }
    }
}
