using AutoMapper;
using ecommerce.Admin.Domain.Dtos.PriceListDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Npgsql;

namespace ecommerce.Admin.Services.Concreate;

public class PriceListService : IPriceListService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<PriceList> _priceListRepository;
    private readonly IRepository<PriceListItem> _priceListItemRepository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IRadzenPagerService<PriceListListDto> _radzenPagerService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "pricelists";

    public PriceListService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
        IRadzenPagerService<PriceListListDto> radzenPagerService,
        ITenantProvider tenantProvider,
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory serviceScopeFactory,
        ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _context = context;
        _priceListRepository = context.GetRepository<PriceList>();
        _priceListItemRepository = context.GetRepository<PriceListItem>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
        _serviceScopeFactory = serviceScopeFactory;
        _roleFilter = roleFilter;
        _permissionService = permissionService;
    }

    private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
    private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
    private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
    private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

    public async Task<IActionResult<Paging<IQueryable<PriceListListDto>>>> GetPriceLists(PageSetting pager)
    {
        IActionResult<Paging<IQueryable<PriceListListDto>>> response = new() { Result = new() };
        try
        {
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            var query = _priceListRepository.GetAll(
                predicate: x => x.Status != (int)EntityStatus.Deleted,
                include: q => q.Include(x => x.Items));
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            // STRICT FILTER: Hide Global PriceLists (BranchId == null) for non-Global Admins
            // Override default "Global Shared" behavior because this user requires strict isolation for PriceLists
            if (!_tenantProvider.IsGlobalAdmin)
            {
                query = query.Where(x => x.BranchId != null);
            }

            var entities = await query.ToListAsync();

            var mapped = entities
                .Select(x => new PriceListListDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    StartDate = x.StartDate,
                    IsActive = x.IsActive,
                    ItemCount = x.Items.Count(i => i.Status != (int)EntityStatus.Deleted),
                    CreatedDate = x.CreatedDate
                })
                .ToList();

            if (mapped?.Count > 0)
            {
                response.Result.Data = mapped
                    .AsQueryable()
                    .OrderByDescending(x => x.Id);
            }

            response.Result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetPriceLists Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<PriceListUpsertDto>> GetPriceListById(int id)
    {
        var rs = new IActionResult<PriceListUpsertDto> { Result = new() };
        try
        {
            if (!await CanView())
            {
                rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return rs;
            }

            var query = _context.DbContext.PriceLists
                .Include(x => x.Items)
                .Where(x => x.Id == id && x.Status != (int)EntityStatus.Deleted);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);

            var entity = await query.FirstOrDefaultAsync();

            if (entity == null)
            {
                // Check existance for security msg
                var exists = await _context.DbContext.PriceLists.IgnoreQueryFilters().AnyAsync(x => x.Id == id && x.Status != (int)EntityStatus.Deleted);
                if (exists)
                {
                     rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return rs;
                }
                return rs;
            }
            
            if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId ?? 0, _context.DbContext))
            {
                    rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                    return rs;
            }

            var dto = new PriceListUpsertDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                StartDate = entity.StartDate,
                IsActive = entity.IsActive,
                Description = entity.Description,
                CustomerId = entity.CustomerId,
                CorporationId = entity.CorporationId,
                BranchId = entity.BranchId,
                WarehouseId = entity.WarehouseId,
                CurrencyId = entity.CurrencyId,
                Items = entity.Items
                    .Where(i => i.Status != (int)EntityStatus.Deleted)
                    .OrderBy(i => i.Order)
                    .Select(i => new PriceListItemUpsertDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        CostPrice = i.CostPrice,
                        SalePrice = i.SalePrice,
                        Order = i.Order
                    }).ToList()
            };

            rs.Result = dto;
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetPriceListById Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> UpsertPriceList(AuditWrapDto<PriceListUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!model.Dto.Id.HasValue)
            {
                if (!await CanCreate())
                {
                    rs.AddError("Ekleme yetkiniz bulunmamaktadır.");
                    return rs;
                }

                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var targetBranchId = model.Dto.BranchId ?? currentBranchId;
                // Aynı şubede aynı isimde liste olmasın
                var duplicate = await _priceListRepository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(pl => pl.Name.ToLower() == model.Dto.Name.ToLower() && pl.BranchId == targetBranchId && pl.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{model.Dto.Name}' isimli fiyat listesi bu şubede zaten mevcut.");
                    return rs;
                }

                // insert (tenant: şube mevcut kullanıcı şubesi, şirket formdan)
                var entity = new PriceList
                {
                    Code = model.Dto.Code,
                    Name = model.Dto.Name,
                    StartDate = model.Dto.StartDate,
                    IsActive = model.Dto.IsActive,
                    Description = model.Dto.Description,
                    CustomerId = model.Dto.CustomerId,
                    CorporationId = model.Dto.CorporationId,
                    BranchId = model.Dto.BranchId ?? currentBranchId,
                    WarehouseId = model.Dto.WarehouseId,
                    CurrencyId = model.Dto.CurrencyId,
                    CreatedDate = DateTime.Now,
                    CreatedId = model.UserId,
                    Status = (int)EntityStatus.Active
                };

                foreach (var item in model.Dto.Items)
                {
                    entity.Items.Add(new PriceListItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        CostPrice = item.CostPrice,
                        SalePrice = item.SalePrice,
                        Order = item.Order,
                        CreatedDate = DateTime.Now,
                        CreatedId = model.UserId,
                        Status = (int)EntityStatus.Active
                    });
                }

                await _priceListRepository.InsertAsync(entity);
                await _context.SaveChangesAsync();

                // Tüm fiyat listeleri (genel + cariye özel): SellerId=1 ile SellerItem sync — product-search'te görünsün
                await SyncSellerItemsFromPriceListAsync(model.Dto.Items, model.UserId, null, model.Dto.CurrencyId);
            }
            else
            {
                if (!await CanEdit())
                {
                    rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                    return rs;
                }

                var query = _context.DbContext.PriceLists
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == model.Dto.Id);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var current = await query.FirstOrDefaultAsync();

                if (current == null)
                {
                     rs.AddError("Fiyat listesi bulunamadı veya yetkiniz yok.");
                     return rs;
                }

                if (!await _roleFilter.CanAccessBranchAsync(current.BranchId ?? 0, _context.DbContext))
                {
                     rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return rs;
                }

                // Check for duplicate name in same branch (excluding current entity)
                var duplicate = await _priceListRepository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(pl => pl.Id != model.Dto.Id && pl.Name.ToLower() == model.Dto.Name.ToLower() && pl.BranchId == current.BranchId && pl.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{model.Dto.Name}' isimli fiyat listesi bu şubede zaten mevcut.");
                    return rs;
                }

                // update header (tenant: şirket/şube/depo formdan)
                await _context.DbContext.PriceLists
                    .Where(x => x.Id == model.Dto.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.Code, model.Dto.Code)
                        .SetProperty(c => c.Name, model.Dto.Name)
                        .SetProperty(c => c.StartDate, model.Dto.StartDate)
                        .SetProperty(c => c.IsActive, model.Dto.IsActive)
                        .SetProperty(c => c.Description, model.Dto.Description)
                        .SetProperty(c => c.CustomerId, model.Dto.CustomerId)
                        .SetProperty(c => c.CorporationId, model.Dto.CorporationId)
                        .SetProperty(c => c.BranchId, model.Dto.BranchId)
                        .SetProperty(c => c.WarehouseId, model.Dto.WarehouseId)
                        .SetProperty(c => c.CurrencyId, model.Dto.CurrencyId)
                        .SetProperty(c => c.ModifiedId, model.UserId)
                        .SetProperty(c => c.ModifiedDate, DateTime.Now));

                // sync items: simple approach -> delete old, insert new
                var existingItems = _context.DbContext.PriceListItems
                    .Where(x => x.PriceListId == model.Dto.Id);

                var oldProductIds = await existingItems.Where(x => x.Status != (int)EntityStatus.Deleted && x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToListAsync();

                await existingItems.ExecuteUpdateAsync(x => x
                    .SetProperty(c => c.Status, (int)EntityStatus.Deleted)
                    .SetProperty(c => c.DeletedId, model.UserId)
                    .SetProperty(c => c.DeletedDate, DateTime.Now));

                foreach (var item in model.Dto.Items)
                {
                    var newItem = new PriceListItem
                    {
                        PriceListId = model.Dto.Id!.Value,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        CostPrice = item.CostPrice,
                        SalePrice = item.SalePrice,
                        Order = item.Order,
                        CreatedDate = DateTime.Now,
                        CreatedId = model.UserId,
                        Status = (int)EntityStatus.Active
                    };

                    await _priceListItemRepository.InsertAsync(newItem);
                }

                await _context.SaveChangesAsync();

                // Tüm fiyat listeleri: SellerId=1 ile SellerItem sync
                var newProductIds = model.Dto.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToList();
                // Sadece genel fiyat listesinde (CustomerId==null) çıkarılan ürünleri SellerItem'dan pasife al
                var removedProductIds = model.Dto.CustomerId == null ? oldProductIds.Except(newProductIds).ToList() : null;
                await SyncSellerItemsFromPriceListAsync(model.Dto.Items, model.UserId, removedProductIds, model.Dto.CurrencyId);
            }

            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Fiyat listesi kaydedildi.");
                return rs;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null)
                {
                    if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        rs.AddError($"'{model.Dto.Name}' isimli fiyat listesi zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
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
            _logger.LogError("UpsertPriceList Exception {Ex}", ex.ToString());
            if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                rs.AddError($"'{model.Dto.Name}' isimli fiyat listesi zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
            }
            else
            {
                rs.AddSystemError(ex.ToString());
            }
            return rs;
        }
    }

    /// <summary>Genel fiyat listesinden SellerId=1 ile SellerItem sync — product-search'te ürünler görünsün.</summary>
    private async Task SyncSellerItemsFromPriceListAsync(List<PriceListItemUpsertDto> items, int userId, List<int>? removedProductIds, int? currencyId = null)
    {
        const int DefaultSellerId = 1;
        var sellerItemRepo = _context.GetRepository<SellerItem>();
        var productRepo = _context.GetRepository<Product>();

        var currencyCode = "TRY";
        if (currencyId.HasValue && currencyId.Value > 0)
        {
            var currency = await _context.DbContext.Currencies.AsNoTracking()
                .Where(c => c.Id == currencyId.Value)
                .Select(c => c.CurrencyCode)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(currency))
                currencyCode = currency;
        }

        var productIds = items.Where(i => i.ProductId.HasValue && i.ProductId.Value > 0).Select(i => i.ProductId!.Value).Distinct().ToList();
        if (productIds.Count == 0 && (removedProductIds == null || !removedProductIds.Any()))
            return;

        // Paket ürünler için ProductSaleItems toplam fiyat
        var packagePrices = await _context.DbContext.ProductSaleItems
            .AsNoTracking()
            .Where(ps => productIds.Contains(ps.RefProductId))
            .GroupBy(ps => ps.RefProductId)
            .Select(g => new { RefProductId = g.Key, TotalPrice = g.Sum(ps => ps.Price) })
            .ToDictionaryAsync(x => x.RefProductId, x => x.TotalPrice);

        var existingSellerItems = await sellerItemRepo.GetAll(ignoreQueryFilters: true)
            .Where(si => si.SellerId == DefaultSellerId && productIds.Contains(si.ProductId))
            .ToDictionaryAsync(si => si.ProductId, si => si);

        foreach (var item in items)
        {
            if (!item.ProductId.HasValue || item.ProductId.Value <= 0) continue;

            var productId = item.ProductId.Value;
            var salePrice = packagePrices.TryGetValue(productId, out var pkgTotal) && pkgTotal > 0
                ? pkgTotal
                : item.SalePrice;

            if (salePrice <= 0) continue;

            if (existingSellerItems.TryGetValue(productId, out var existing))
            {
                existing.SalePrice = salePrice;
                existing.CostPrice = item.CostPrice;
                existing.Currency = currencyCode;
                existing.Stock = existing.Stock <= 0 ? 999 : existing.Stock;
                existing.Status = (int)EntityStatus.Active;
                existing.ModifiedDate = DateTime.Now;
                existing.ModifiedId = userId;
                sellerItemRepo.Update(existing);
            }
            else
            {
                var newSi = new SellerItem
                {
                    SellerId = DefaultSellerId,
                    ProductId = productId,
                    SalePrice = salePrice,
                    CostPrice = item.CostPrice,
                    Stock = 999,
                    Status = (int)EntityStatus.Active,
                    Currency = currencyCode,
                    Unit = "adet",
                    CreatedDate = DateTime.Now,
                    CreatedId = userId
                };
                await sellerItemRepo.InsertAsync(newSi);
            }
        }

        // Listeden çıkarılan ürünler: SellerItem pasife al
        if (removedProductIds != null && removedProductIds.Any())
        {
            var toDeactivate = await sellerItemRepo.GetAll(ignoreQueryFilters: true)
                .Where(si => si.SellerId == DefaultSellerId && removedProductIds.Contains(si.ProductId))
                .ToListAsync();
            foreach (var si in toDeactivate)
            {
                si.Status = (int)EntityStatus.Passive;
                si.ModifiedDate = DateTime.Now;
                si.ModifiedId = userId;
                sellerItemRepo.Update(si);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IActionResult<Empty>> DeletePriceList(AuditWrapDto<PriceListDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!await CanDelete())
            {
                rs.AddError("Silme yetkiniz bulunmamaktadır.");
                return rs;
            }

            var query = _context.DbContext.PriceLists
                .IgnoreQueryFilters()
                .Where(x => x.Id == model.Dto.Id);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            var current = await query.FirstOrDefaultAsync();

            if (current == null)
            {
                 rs.AddError("Fiyat listesi bulunamadı veya yetkiniz yok.");
                 return rs;
            }

            if (!await _roleFilter.CanAccessBranchAsync(current.BranchId ?? 0, _context.DbContext))
            {
                 rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                 return rs;
            }

            await _context.DbContext.PriceLists
                .Where(f => f.Id == model.Dto.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                    .SetProperty(a => a.DeletedDate, DateTime.Now)
                    .SetProperty(a => a.DeletedId, model.UserId));

            await _context.SaveChangesAsync();

            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Fiyat listesi silindi.");
                return rs;
            }

            if (lastResult.Exception != null)
                rs.AddError(lastResult.Exception.ToString());

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("DeletePriceList Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
}


