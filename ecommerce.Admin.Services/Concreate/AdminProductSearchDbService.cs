using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate
{
    /// <summary>
    /// DB tabanlı ürün arama servisi - Elasticsearch yerine doğrudan veritabanı kullanır.
    /// Plasiyer ekranı için kullanılır.
    /// </summary>
    public class AdminProductSearchDbService : IAdminProductSearchService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _uow;
        private readonly ISearchSynonymService _synonymService;
        private readonly ISellerService _sellerService;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<AdminProductSearchDbService> _logger;

        public AdminProductSearchDbService(
            IUnitOfWork<ApplicationDbContext> uow,
            ISearchSynonymService synonymService,
            ISellerService sellerService,
            ITenantProvider tenantProvider,
            ILogger<AdminProductSearchDbService> logger)
        {
            _uow = uow;
            _synonymService = synonymService;
            _sellerService = sellerService;
            _tenantProvider = tenantProvider;
            _logger = logger;
        }

        public async Task<IActionResult<List<SellerProductViewModel>>> SearchAsync(string keyword, bool onlyInStock = false)
        {
            var filter = new SearchFilterReguestDto
            {
                Search = keyword,
                OnlyInStock = onlyInStock,
                Page = 1,
                PageSize = 200
            };
            var result = await GetByFilterPagingAsync(filter);
            if (!result.Ok || result.Result == null)
                return OperationResult.CreateResult<List<SellerProductViewModel>>();
            return OperationResult.CreateResult(result.Result.Data ?? new List<SellerProductViewModel>());
        }

        public async Task<IActionResult<Paging<List<SellerProductViewModel>>>> GetByFilterPagingAsync(SearchFilterReguestDto filter)
        {
            var rs = OperationResult.CreateResult<Paging<List<SellerProductViewModel>>>();
            try
            {
                var db = _uow.DbContext;
                List<int>? allowedSellerIds = null;
                if (_tenantProvider.IsMultiTenantEnabled)
                    allowedSellerIds = await _sellerService.GetAllSellerIds();

                // Sadece SellerId=1 (genel fiyat listesinden gelen ürünler) — ilanlardan satış kaldırıldı
                const int DefaultSellerId = 1;
                var query = db.SellerItems
                    .AsNoTracking()
                    .Include(si => si.Product)
                        .ThenInclude(p => p!.Brand)
                    .Include(si => si.Product)
                        .ThenInclude(p => p!.Category)
                    .Include(si => si.Product)
                        .ThenInclude(p => p!.ProductGroupCodes)
                    .Include(si => si.Product)
                        .ThenInclude(p => p!.ProductImage)
                    .Include(si => si.Seller)
                    .Where(si => si.SellerId == DefaultSellerId)
                    .Where(si => si.Status == (int)EntityStatus.Active)
                    .Where(si => si.Product != null && si.Product.Status == (int)EntityStatus.Active)
                    .Where(si => si.Product!.IsSoldOnWeb)
                    .Where(si => si.SalePrice > 0);

                if (allowedSellerIds != null)
                {
                    if (!allowedSellerIds.Contains(DefaultSellerId))
                    {
                        rs.Result = new Paging<List<SellerProductViewModel>> { Data = new List<SellerProductViewModel>(), DataCount = 0 };
                        return rs;
                    }
                }

                if (filter.OnlyInStock)
                    query = query.Where(si => si.Stock > 0);

                if (filter.MinPrice.HasValue && filter.MinPrice.Value > 0)
                    query = query.Where(si => si.SalePrice >= (decimal)filter.MinPrice.Value);
                if (filter.MaxPrice.HasValue)
                    query = query.Where(si => si.SalePrice <= (decimal)filter.MaxPrice.Value);

                if (filter.CategoryIds != null && filter.CategoryIds.Any())
                    query = query.Where(si => si.Product!.CategoryId != null && filter.CategoryIds.Contains(si.Product.CategoryId.Value));
                if (filter.BrandIds != null && filter.BrandIds.Any())
                    query = query.Where(si => filter.BrandIds.Contains(si.Product!.BrandId));
                if (filter.ProductIds != null && filter.ProductIds.Any())
                    query = query.Where(si => filter.ProductIds.Contains(si.ProductId));

                if (filter.OnlyWithImage)
                    query = query.Where(si => si.Product!.ProductImage != null && si.Product.ProductImage.Any());

                // Sadece ürün adı ile arama
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var terms = filter.Search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var term in terms)
                    {
                        if (string.IsNullOrWhiteSpace(term) || term.Length < 2) continue;
                        var t = term.ToLower();
                        query = query.Where(si => si.Product!.Name != null && si.Product.Name.ToLower().Contains(t));
                    }
                }

                var totalCount = await query.CountAsync();

                var pageSize = filter.PageSize > 0 ? filter.PageSize : 50;
                var skip = (filter.Page - 1) * pageSize;

                var items = await query
                    .OrderBy(si => si.Stock > 0 ? 0 : 1)
                    .ThenBy(si => si.SalePrice)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var productIds = items.Select(si => si.ProductId).Distinct().ToList();
                var oemDetailsByProduct = await db.ProductOemDetails.AsNoTracking()
                    .Where(pod => productIds.Contains(pod.ProductId))
                    .GroupBy(pod => pod.ProductId)
                    .ToDictionaryAsync(g => g.Key, g => g.First());

                var viewModels = items.Select(si => MapToViewModel(si, oemDetailsByProduct.GetValueOrDefault(si.ProductId))).ToList();

                rs.Result = new Paging<List<SellerProductViewModel>>
                {
                    Data = viewModels,
                    DataCount = totalCount,
                    TotalRawCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetByFilterPagingAsync DB error");
                rs.AddError($"Arama hatası: {ex.Message}");
            }
            return rs;
        }

        public async Task<IActionResult<SearchFilterAggregations>> GetSearchAggregationsAsync(SearchFilterReguestDto filter)
        {
            var rs = OperationResult.CreateResult<SearchFilterAggregations>();
            try
            {
                var db = _uow.DbContext;
                List<int>? allowedSellerIds = null;
                if (_tenantProvider.IsMultiTenantEnabled)
                    allowedSellerIds = await _sellerService.GetAllSellerIds();

                const int DefaultSellerId = 1;
                var baseQuery = db.SellerItems
                    .AsNoTracking()
                    .Include(si => si.Product)
                    .Where(si => si.SellerId == DefaultSellerId)
                    .Where(si => si.Status == (int)EntityStatus.Active)
                    .Where(si => si.Product != null && si.Product.Status == (int)EntityStatus.Active)
                    .Where(si => si.Product!.IsSoldOnWeb)
                    .Where(si => si.SalePrice > 0);

                if (allowedSellerIds != null && !allowedSellerIds.Contains(DefaultSellerId))
                    baseQuery = baseQuery.Where(si => false); // Hiçbir sonuç döndürme

                if (filter.OnlyInStock) baseQuery = baseQuery.Where(si => si.Stock > 0);
                if (filter.CategoryIds != null && filter.CategoryIds.Any())
                    baseQuery = baseQuery.Where(si => si.Product!.CategoryId != null && filter.CategoryIds.Contains(si.Product.CategoryId!.Value));
                if (filter.BrandIds != null && filter.BrandIds.Any())
                    baseQuery = baseQuery.Where(si => filter.BrandIds.Contains(si.Product!.BrandId));
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var t = filter.Search.Trim().ToLower();
                    baseQuery = baseQuery.Where(si => si.Product!.Name != null && si.Product.Name.ToLower().Contains(t));
                }

                var aggs = new SearchFilterAggregations();
                rs.Result = aggs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSearchAggregationsAsync DB error");
                rs.AddError(ex.Message);
            }
            return rs;
        }

        public async Task<IActionResult<List<SellerProductViewModel>>> GetSimilarProductsAsync(string oemCode)
            => await GetSimilarProductsAsync(new List<string> { oemCode });

        public async Task<IActionResult<List<SellerProductViewModel>>> GetSimilarProductsAsync(List<string> oemCodes)
        {
            var rs = OperationResult.CreateResult<List<SellerProductViewModel>>();
            try
            {
                if (oemCodes == null || !oemCodes.Any())
                {
                    rs.Result = new List<SellerProductViewModel>();
                    return rs;
                }

                var searchTerms = oemCodes.Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToList();
                if (!searchTerms.Any()) { rs.Result = new List<SellerProductViewModel>(); return rs; }

                var db = _uow.DbContext;
                List<int>? allowedSellerIds = null;
                if (_tenantProvider.IsMultiTenantEnabled)
                    allowedSellerIds = await _sellerService.GetAllSellerIds();

                const int DefaultSellerId = 1;
                var query = db.SellerItems
                    .AsNoTracking()
                    .Include(si => si.Product).ThenInclude(p => p!.Brand)
                    .Include(si => si.Product).ThenInclude(p => p!.Category)
                    .Include(si => si.Product).ThenInclude(p => p!.ProductGroupCodes)
                    .Include(si => si.Product).ThenInclude(p => p!.ProductImage)
                    .Include(si => si.Seller)
                    .Where(si => si.SellerId == DefaultSellerId)
                    .Where(si => si.Status == (int)EntityStatus.Active)
                    .Where(si => si.Product != null && si.Product.Status == (int)EntityStatus.Active)
                    .Where(si => si.Product!.IsSoldOnWeb)
                    .Where(si => si.SalePrice > 0);

                foreach (var term in searchTerms)
                {
                    if (term.Length < 2) continue;
                    var t = term.ToLower();
                    query = query.Where(si => si.Product!.Name != null && si.Product.Name.ToLower().Contains(t));
                }

                if (allowedSellerIds != null && !allowedSellerIds.Contains(DefaultSellerId))
                    query = query.Where(si => false);

                var items = await query.Take(50).ToListAsync();
                var productIds = items.Select(si => si.ProductId).Distinct().ToList();
                var oemDetailsByProduct = await db.ProductOemDetails.AsNoTracking()
                    .Where(pod => productIds.Contains(pod.ProductId))
                    .GroupBy(pod => pod.ProductId)
                    .ToDictionaryAsync(g => g.Key, g => g.First());
                rs.Result = items.Select(si => MapToViewModel(si, oemDetailsByProduct.GetValueOrDefault(si.ProductId))).OrderByDescending(v => v.Stock > 0).ToList();
            }
            catch (Exception ex)
            {
                rs.AddError(ex.Message);
            }
            return rs;
        }

        private static SellerProductViewModel MapToViewModel(SellerItem si, ProductOemDetail? pod)
        {
            var p = si.Product!;
            var firstOem = p.ProductGroupCodes?.FirstOrDefault()?.OemCode ?? pod?.Oem;
            var oemList = new List<string>();
            if (p.ProductGroupCodes != null)
                oemList.AddRange(p.ProductGroupCodes.Where(pgc => !string.IsNullOrEmpty(pgc.OemCode)).Select(pgc => pgc.OemCode));
            if (pod != null && !string.IsNullOrEmpty(pod.Oem) && !oemList.Contains(pod.Oem))
                oemList.Add(pod.Oem);
            oemList = oemList.Distinct().ToList();
            var mainImage = p.ProductImage?.OrderBy(pi => pi.Order).FirstOrDefault()?.FileName;

            return new SellerProductViewModel
            {
                SellerItemId = si.Id,
                ProductId = p.Id,
                ProductName = p.Name,
                ProductDescription = p.Description,
                ProductBarcode = p.Barcode,
                DocumentUrl = p.DocumentUrl,
                MainImageUrl = mainImage,
                Stock = (int)si.Stock,
                SalePrice = (double)si.SalePrice,
                CostPrice = (double)si.CostPrice,
                Currency = si.Currency,
                Unit = si.Unit,
                SellerId = si.SellerId,
                SellerName = si.Seller?.Name,
                SellerModifiedDate = si.ModifiedDate,
                SourceId = si.SourceId,
                Step = (double)si.Step,
                MinSaleAmount = (double)si.MinSaleAmount,
                MaxSaleAmount = (double)si.MaxSaleAmount,
                PartNumber = firstOem,
                OemCode = oemList.Any() ? oemList : null,
                IsEquivalent = string.IsNullOrWhiteSpace(firstOem) && oemList.Any(),
                IsPackageProduct = p.IsPackageProduct,
                DotPartName = pod?.Name,
                ManufacturerName = si.ManufacturerName ?? pod?.ManufacturerName,
                VehicleTypeName = pod?.VehicleTypeName,
                DotPartDescription = pod?.Name,
                BaseModelName = pod?.BaseModelName,
                NetPrice = (double?)(pod?.NetPrice ?? 0),
                PriceDate = null,
                DatProcessNumber = !string.IsNullOrEmpty(pod?.DatProcessNumber) ? new List<string> { pod.DatProcessNumber } : null,
                VehicleType = pod?.VehicleType,
                ManufacturerKey = pod?.ManufacturerKey?.ToString(),
                BaseModelKey = pod?.BaseModelKey?.ToString(),
                Brand = p.Brand != null ? new BrandDto { Id = p.Brand.Id, Name = p.Brand.Name, Status = p.Brand.Status } : null,
                Categories = p.Category != null ? new List<CategoryDto> { new CategoryDto { Id = p.Category.Id, Name = p.Category.Name } } : new List<CategoryDto>(),
                Images = p.ProductImage?.Select(pi => new ProductImageDto { Id = pi.Id, ProductId = pi.ProductId, FileName = pi.FileName, FileGuid = pi.FileGuid }).ToList() ?? new List<ProductImageDto>(),
                SimilarProductCount = 0
            };
        }
    }
}
