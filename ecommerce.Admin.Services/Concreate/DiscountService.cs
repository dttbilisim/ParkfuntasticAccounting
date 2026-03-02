using AutoMapper;
using ecommerce.Admin.Domain.Dtos.DiscountDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Identity;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Npgsql;

namespace ecommerce.Admin.Domain.Concreate
{
    public class DiscountService : IDiscountService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Discount> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly CurrentUser _currentUser;
        private readonly FileHelper _fileHelper;
        private readonly IAdminProductSearchService _productSearchService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "discounts";

        public DiscountService(
            IUnitOfWork<ApplicationDbContext> context,
            IMapper mapper,
            ILogger logger,
            CurrentUser currentUser,
            FileHelper fileHelper,
            IAdminProductSearchService productSearchService,
            ITenantProvider tenantProvider,
            IHttpContextAccessor httpContextAccessor,
            IServiceScopeFactory serviceScopeFactory,
            ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
            _fileHelper = fileHelper;
            _productSearchService = productSearchService;
            _repository = context.GetRepository<Discount>();
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _serviceScopeFactory = serviceScopeFactory;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Empty>> DeleteDiscount(DiscountDeleteDto dto)
        {
            var response = OperationResult.CreateResult<Empty>();

            try
            {
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;

                var discount = await _context.DbContext.Discounts.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == dto.Id);
                if (discount == null)
                {
                    response.AddError("İndirim bulunamadı");
                    return response;
                }

                if (!isGlobalAdmin)
                {
                    var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         var isAllowed = await _context.DbContext.UserBranches
                            .AnyAsync(ub => ub.UserId == userId && ub.BranchId == discount.BranchId && ub.Status == (int)EntityStatus.Active);
                         if (!isAllowed)
                         {
                             response.AddError("Bu indirimi silme yetkiniz yok.");
                             return response;
                         }
                    }
                }

                discount.Status = (int)EntityStatus.Deleted;
                discount.DeletedDate = DateTime.Now;
                discount.DeletedId = _currentUser.GetId();

                await _context.SaveChangesAsync();
                response.AddSuccess("Başarıyla silindi");
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteOffer Exception: {ex}");
                response.AddSystemError(ex.ToString());
            }

            return response;
        }

        public async Task<IActionResult<DiscountUpsertDto>> GetDiscountById(int Id)
        {
            var response = OperationResult.CreateResult<DiscountUpsertDto>();

            try
            {
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;

                var discount = await _repository.GetFirstOrDefaultAsync(
                    predicate: f => f.Id == Id 
                        && (isGlobalAdmin ? (currentBranchId == 0 || f.BranchId == currentBranchId) : true),
                    include: q => q.Include(d => d.CompanyCoupons).ThenInclude(c => c.Company),
                    ignoreQueryFilters: true
                );

                if (discount == null)
                {
                    response.AddError("İndirim bulunamadı");
                    return response;
                }

                if (!isGlobalAdmin)
                {
                    var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         var isAllowed = await _context.DbContext.UserBranches
                            .AnyAsync(ub => ub.UserId == userId && ub.BranchId == discount.BranchId && ub.Status == (int)EntityStatus.Active);
                         if (!isAllowed)
                         {
                             response.AddError("Bu indirimi görme yetkiniz yok.");
                             return response;
                         }
                         
                         if (currentBranchId > 0 && discount.BranchId != currentBranchId)
                         {
                             response.AddError("İndirim seçili şubeye ait değil.");
                             return response;
                         }
                    }
                }

                response.Result = _mapper.Map<DiscountUpsertDto>(discount);

                response.Result.Description = HtmlHelper.ModifyHtmlContentImages(_fileHelper, response.Result.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetOfferById Exception " + ex);
                response.AddSystemError(ex.ToString());
            }

            return response;
        }

        public async Task<IActionResult<List<DiscountListDto>>> GetDiscounts()
        {
            var response = OperationResult.CreateResult<List<DiscountListDto>>();

            try
            {
                var query = _repository.GetAll(true)
                    .Where(s => s.Status != (int)EntityStatus.Deleted);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var discounts = await query.IgnoreQueryFilters().ToListAsync();

                response.Result = _mapper.Map<List<DiscountListDto>>(discounts);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetOffers Exception " + ex);
                response.AddSystemError(ex.ToString());
            }

            return response;
        }

        public async Task<IActionResult<Paging<List<DiscountListDto>>>> GetDiscounts(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<DiscountListDto>>>();

            try
            {
                // Security Logic
                var query = _repository.GetAll(true)
                    .Where(s => s.Status != (int)EntityStatus.Deleted);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                response.Result = await query
                    .IgnoreQueryFilters()
                    .ToPagedResultAsync<DiscountListDto>(pager, _mapper);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetDiscounts Exception " + ex);
                response.AddSystemError(ex.ToString());
            }

            return response;
        }

        public async Task<IActionResult<Empty>> UpsertDiscount(DiscountUpsertDto dto)
        {
            var response = OperationResult.CreateResult<Empty>();

            try
            {
                if (dto.Id.HasValue)
                {
                    // Mevcudu NoTracking ile getir
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: r => r.Id == dto.Id && r.Status != (int)EntityStatus.Deleted,
                        include: q => q.Include(d => d.CompanyCoupons),
                        disableTracking: true);
                    if (current == null)
                    {
                        response.AddError("İndirim bulunamadı");
                        return response;
                    }

                    // Check for duplicate name in same branch (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(d => d.Id != dto.Id && d.Name.ToLower() == dto.Name.ToLower() && d.BranchId == current.BranchId && d.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli indirim bu şubede zaten mevcut.");
                        return response;
                    }

                    // DTO'dan yeni bir entity oluştur ve audit/immutable alanları koru
                    var updated = _mapper.Map<Discount>(dto);
                    updated.Id = current.Id;
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Description = HtmlHelper.ModifyHtmlContentImages(_fileHelper, updated.Description, true);
                    updated.ModifiedId = _currentUser.GetId();
                    updated.ModifiedDate = DateTime.Now;
                    updated.AssignedEntityIds = updated.AssignedEntityIds ?? new List<int>();
                    updated.AssignedSellerIds = updated.AssignedSellerIds ?? new List<int>();
                    updated.GiftProductIds = updated.GiftProductIds ?? new List<int>();
                    // Navigasyonları bu akışta güncellemiyoruz
                    updated.CompanyCoupons = current.CompanyCoupons;

                    if (_tenantProvider.IsMultiTenantEnabled)
                    {
                        updated.BranchId = current.BranchId; // Preserve branch
                    }

                    _repository.AttachAsModified(updated, excludeNavigations: true);
                    _logger.LogInformation("UpsertDiscount(Update): Attached as Modified");
                }
                else
                {
                    // Check for duplicate name in current branch
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(d => d.Name.ToLower() == dto.Name.ToLower() && d.BranchId == currentBranchId && d.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli indirim bu şubede zaten mevcut.");
                        return response;
                    }

                    var insert = _mapper.Map<Discount>(dto);
                    insert.Description = HtmlHelper.ModifyHtmlContentImages(_fileHelper, insert.Description, true);
                    insert.CreatedId = _currentUser.GetId();
                    insert.CreatedDate = DateTime.Now;
                    if (insert.Status == 0)
                    {
                        insert.Status = (int)EntityStatus.Active;
                    }
                    if (_tenantProvider.IsMultiTenantEnabled)
                    {
                        insert.BranchId = currentBranchId;
                    }
                    insert.AssignedEntityIds = insert.AssignedEntityIds ?? new List<int>();
                    insert.AssignedSellerIds = insert.AssignedSellerIds ?? new List<int>();
                    insert.GiftProductIds = insert.GiftProductIds ?? new List<int>();
                    await _repository.InsertAsync(insert);
                }

                var affected = await _context.SaveChangesAsync();
                if (affected <= 0)
                {
                    _logger.LogWarning("UpsertDiscount finished but no rows were affected. DTO Id: {Id}", dto.Id);
                }

                var lastResult = _context.LastSaveChangesResult;
                if (!lastResult.IsOk)
                {
                    if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        response.AddError($"'{dto.Name}' isimli indirim zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                    }
                    else
                    {
                        _logger.LogError($"UpsertDiscount Exception {lastResult.Exception}");
                        response.AddError(lastResult.Exception?.ToString() ?? "Bir hata oluştu");
                    }
                }
                else
                {
                    _logger.LogInformation("UpsertDiscount succeeded.");
                    response.AddSuccess("Kayıt işlemi başarılı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpsertDiscount Exception {ex}");
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    response.AddError($"'{dto.Name}' isimli indirim zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                }
                else
                {
                    response.AddSystemError(ex.ToString());
                }
            }

            return response;
        }

        public async Task<string> GenerateCouponCode()
        {
            while (true)
            {
                var couponCode = GenerateCouponKeyHelper.GenerateCode();

                var checkCoupon = await _repository.GetAll(true)
                    .Include(d => d.CompanyCoupons)
                    .AnyAsync(
                        x => x.CouponCode == couponCode
                             && x.CompanyCoupons.Any(c => c.CouponCode == couponCode)
                             && x.Status != (int) EntityStatus.Deleted
                    );

                if (!checkCoupon)
                {
                    return couponCode;
                }
            }
        }

        public async Task<IActionResult<List<DiscountWithProductsDto>>> GetActiveDiscountsWithProductsAsync()
        {
            var response = OperationResult.CreateResult<List<DiscountWithProductsDto>>();

            try
            {
                var now = DateTime.UtcNow;

                // DEBUG: Önce tüm aktif indirimleri çek (status kontrolü)
                var allActiveDiscounts = await _repository.GetAll(true)
                    .Where(d => d.Status == (int)EntityStatus.Active)
                    .ToListAsync();

                _logger.LogInformation($"GetActiveDiscountsWithProductsAsync: Found {allActiveDiscounts.Count} active discounts (Status=1)");

                // Aktif indirimleri çek (tarih kontrolü + status kontrolü)
                // Daha esnek: AssignedEntityIds null veya boş olabilir, sadece DiscountType kontrolü yap
                // RequiresCouponCode kontrolü yapmıyoruz - kullanıcı coupon code girmese bile kampanyaları görebilir
                var activeDiscounts = allActiveDiscounts
                    .Where(d => (!d.StartDate.HasValue || d.StartDate.Value <= now) &&
                               (!d.EndDate.HasValue || d.EndDate.Value >= now) &&
                               (d.DiscountType == DiscountType.AssignedToProducts ||
                                d.DiscountType == DiscountType.AssignedToCategories ||
                                d.DiscountType == DiscountType.AssignedToBrands))
                    .ToList();

                _logger.LogInformation($"GetActiveDiscountsWithProductsAsync: After date and type filter: {activeDiscounts.Count} discounts");

                // AssignedEntityIds kontrolü - eğer null veya boşsa, sadece log'la ama ekleme
                var discountsWithEntities = activeDiscounts
                    .Where(d => d.AssignedEntityIds != null && d.AssignedEntityIds.Any())
                    .ToList();

                _logger.LogInformation($"GetActiveDiscountsWithProductsAsync: After AssignedEntityIds filter: {discountsWithEntities.Count} discounts");

                // Eğer AssignedEntityIds olan yoksa, tüm aktif indirimleri göster (ürün listesi boş olabilir)
                var finalDiscounts = discountsWithEntities.Any() ? discountsWithEntities : activeDiscounts;

                if (finalDiscounts.Count == 0)
                {
                    _logger.LogWarning("GetActiveDiscountsWithProductsAsync: No discounts found. Checking all discounts in DB...");
                    var allDiscounts = await _repository.GetAll(true).ToListAsync();
                    _logger.LogInformation($"Total discounts in DB: {allDiscounts.Count}");
                    foreach (var d in allDiscounts.Take(10))
                    {
                        _logger.LogInformation($"Discount ID: {d.Id}, Name: {d.Name}, Status: {d.Status}, StartDate: {d.StartDate}, EndDate: {d.EndDate}, DiscountType: {d.DiscountType}, AssignedEntityIds Count: {d.AssignedEntityIds?.Count ?? 0}, RequiresCouponCode: {d.RequiresCouponCode}");
                    }
                }

                activeDiscounts = finalDiscounts;

                var result = new List<DiscountWithProductsDto>();

                foreach (var discount in activeDiscounts)
                {
                    var dto = new DiscountWithProductsDto
                    {
                        Id = discount.Id,
                        Name = discount.Name,
                        Description = discount.Description,
                        ImagePath = discount.ImagePath,
                        CampaignLink = discount.CampaignLink,
                        DiscountType = discount.DiscountType,
                        UsePercentage = discount.UsePercentage,
                        DiscountPercentage = discount.DiscountPercentage,
                        DiscountAmount = discount.DiscountAmount,
                        StartDate = discount.StartDate,
                        EndDate = discount.EndDate,
                        AssignedEntityIds = discount.AssignedEntityIds
                    };

                    // DiscountType'a göre ürünleri bul
                    var productFilter = new SearchFilterReguestDto
                    {
                        Page = 1,
                        PageSize = 20, // Maksimum 20 ürün göster
                        Search = "", // Boş arama
                        Sort = ProductFilter.ByPriceAsc // Varsayılan sıralama
                    };

                    switch (discount.DiscountType)
                    {
                        case DiscountType.AssignedToProducts:
                            // Ürün ID'lerine göre filtrele - Elasticsearch'te ProductId field'ı var
                            // Bu durumda SearchFilterReguestDto'da ProductIds yok, 
                            // bu yüzden önce tüm ürünleri çekip sonra filtreleyeceğiz
                            // VEYA SearchFilterReguestDto'ya ProductIds ekleyeceğiz
                            // Şimdilik tüm ürünleri çekip memory'de filtreleyelim
                            break;

                        case DiscountType.AssignedToCategories:
                            // Kategori ID'lerine göre filtrele
                            if (discount.AssignedEntityIds != null && discount.AssignedEntityIds.Any())
                            {
                                productFilter.CategoryIds = discount.AssignedEntityIds;
                            }
                            break;

                        case DiscountType.AssignedToBrands:
                            // Marka ID'lerine göre filtrele
                            if (discount.AssignedEntityIds != null && discount.AssignedEntityIds.Any())
                            {
                                productFilter.BrandIds = discount.AssignedEntityIds;
                            }
                            break;
                    }

                    // Ürünleri çek
                    var productSearchResult = await _productSearchService.GetByFilterPagingAsync(productFilter);

                    if (productSearchResult.Ok && productSearchResult.Result != null)
                    {
                        var products = productSearchResult.Result.Data ?? new List<SellerProductViewModel>();

                        // Eğer AssignedToProducts ise, ProductId'ye göre filtrele
                        if (discount.DiscountType == DiscountType.AssignedToProducts &&
                            discount.AssignedEntityIds != null && discount.AssignedEntityIds.Any())
                        {
                            products = products.Where(p => discount.AssignedEntityIds.Contains(p.ProductId)).ToList();
                        }

                        dto.Products = products.Take(20).ToList(); // Maksimum 20 ürün
                    }

                    result.Add(dto);
                }

                response.Result = result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetActiveDiscountsWithProductsAsync Exception: {ex}");
                response.AddSystemError(ex.ToString());
            }

            return response;
        }

    }
}