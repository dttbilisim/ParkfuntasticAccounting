using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Interfaces;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Domain.Shared.Models;
using ecommerce.Domain.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Nest;

namespace ecommerce.Admin.Services.Concreate
{
    public class AdminProductSearchService : IAdminProductSearchService
    {
        private readonly IElasticClient _client;
        private readonly ISearchSynonymService _synonymService;
        private readonly ISellerService _sellerService;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<AdminProductSearchService> _logger;

        private class DeduplicateResult
        {
            public List<SellerProductElasticDto> Kept { get; set; } = new();
            public Dictionary<string, int> GroupCounts { get; set; } = new();
        }

        public AdminProductSearchService(
            IElasticClient client, 
            ISearchSynonymService synonymService, 
            ISellerService sellerService, 
            ITenantProvider tenantProvider,
            ILogger<AdminProductSearchService> logger)
        {
            _client = client;
            _synonymService = synonymService;
            _sellerService = sellerService;
            _tenantProvider = tenantProvider;
            _logger = logger;
        }

        public async Task<IActionResult<List<SellerProductViewModel>>> SearchAsync(string keyword, bool onlyInStock = false)
        {
            var rs = OperationResult.CreateResult<List<SellerProductViewModel>>();
            try
            {
                if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
                {
                    rs.Result = new List<SellerProductViewModel>();
                    return rs;
                }

                var searchWords = keyword.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);



                // Fetch Dynamic Metadata
                var metadata = await _synonymService.GetSearchMetadataAsync();

                // Multi-Tenant Isolation
                List<int>? allowedSellerIds = null;
                if (_tenantProvider.IsMultiTenantEnabled)
                {
                    allowedSellerIds = await _sellerService.GetAllSellerIds();
                }

                var searchResponse = await _client.SearchAsync<SellerProductElasticDto>(s => s
                    .Index("sellerproduct_index")
                    .Size(200)
                    .Sort(st => st
                        .Script(sc => sc
                            .Type("number")
                            .Descending()
                            .Script(scr => scr.Source("doc.containsKey('Stock') && !doc['Stock'].empty && doc['Stock'].value > 0 ? 1 : 0"))
                        )
                        .Descending("_score")
                        .Ascending("SalePrice")
                    )
                    .Query(q => q.Bool(b =>
                    {
                        var boolDescriptor = b;

                        // Use Centralized Search Logic with Dynamic Metadata
                        var smartQuery = SearchEngineHelper.BuildSmartSearchQuery(keyword, metadata);
                        
                        boolDescriptor.Must(smartQuery);

                        var filters = new List<QueryContainer>();
                        filters.Add(new NumericRangeQuery { Field = "SalePrice", GreaterThan = 0 });
                        if (onlyInStock)
                        {
                            filters.Add(new NumericRangeQuery { Field = "Stock", GreaterThan = 0 });
                        }

                        // Tenant Filter
                        if (allowedSellerIds != null)
                        {
                            if (allowedSellerIds.Any())
                            {
                                filters.Add(new TermsQuery { Field = "SellerId", Terms = allowedSellerIds.Cast<object>() });
                            }
                            else
                            {
                                // If tenant is enabled but no allowed sellers, return nothing
                                filters.Add(new MatchNoneQuery());
                            }
                        }

                        boolDescriptor.Filter(filters.ToArray());

                        return boolDescriptor;
                    }))
                );

                if (!searchResponse.IsValid)
                {
                    rs.AddError($"Search failed: {searchResponse.OriginalException?.Message}");
                    return rs;
                }

                // De-duplicate items based on setting 
                List<SellerProductElasticDto> products;
                if (metadata != null && !metadata.ShouldGroupOems)
                {
                    products = searchResponse.Documents.ToList();
                }
                else
                {
                    var dedup = CorrectAndDeduplicate(searchResponse.Documents.ToList(), metadata?.ShouldGroupOems ?? false);
                    products = dedup.Kept;
                }
                // Normal arama - similar count dahil (backward compatible)
                rs.Result = await JoinRelatedData(products, skipSimilarCount: false);
            }
            catch (Exception e)
            {
                rs.AddError($"SearchAsync error: {e.Message}");
            }

            return rs;
        }

        public async Task<IActionResult<Paging<List<SellerProductViewModel>>>> GetByFilterPagingAsync(SearchFilterReguestDto filter)
        {
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var rs = OperationResult.CreateResult<Paging<List<SellerProductViewModel>>>();
            try
            {
                // Fetch Dynamic Metadata
                var metadataStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var metadata = await _synonymService.GetSearchMetadataAsync();
                metadataStopwatch.Stop();
                _logger.LogInformation("[PERF] Metadata çekme süresi: {Duration}ms", metadataStopwatch.ElapsedMilliseconds);

                // Multi-Tenant Isolation
                var tenantStopwatch = System.Diagnostics.Stopwatch.StartNew();
                List<int>? allowedSellerIds = null;
                if (_tenantProvider.IsMultiTenantEnabled)
                {
                    allowedSellerIds = await _sellerService.GetAllSellerIds();
                }
                tenantStopwatch.Stop();
                _logger.LogInformation("[PERF] Tenant kontrolü süresi: {Duration}ms, AllowedSellers: {Count}", 
                    tenantStopwatch.ElapsedMilliseconds, allowedSellerIds?.Count ?? 0);

                var queryBuildStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var boolQuery = BuildBaseQuery(filter, metadata, allowedSellerIds);
                ApplyVehicleFilters(boolQuery, filter);
                queryBuildStopwatch.Stop();
                _logger.LogInformation("[PERF] Query oluşturma süresi: {Duration}ms, BrandIds: {BrandCount}, CatIds: {CatCount}, MKey: {MKey}, BKey: {BKey}", 
                    queryBuildStopwatch.ElapsedMilliseconds, 
                    filter.BrandIds?.Count ?? 0, 
                    filter.CategoryIds?.Count ?? 0,
                    filter.ManufacturerKey ?? "null",
                    filter.BaseModelKey ?? "null");

                var esStopwatch = System.Diagnostics.Stopwatch.StartNew();
                _logger.LogInformation("[PERF-ES] Elasticsearch sorgusu başlatılıyor...");
                
                // DEBUG: DatProcessNumbers ile arama yapılıyorsa query'yi logla
                if (filter.DatProcessNumbers != null && filter.DatProcessNumbers.Any())
                {
                    _logger.LogInformation("[DEBUG-DPN] Elasticsearch'e gönderilen query - DatProcessNumber sayısı: {Count}, İlk 10: {Codes}", 
                        filter.DatProcessNumbers.Count,
                        string.Join(", ", filter.DatProcessNumbers.Take(10)));
                }
                // DEBUG: OEM kodları ile arama yapılıyorsa query'yi logla
                else if (filter.OemCodes != null && filter.OemCodes.Any())
                {
                    _logger.LogInformation("[DEBUG-OEM] Elasticsearch'e gönderilen query - OEM kod sayısı: {Count}", filter.OemCodes.Count);
                }
                
                var result = await _client.SearchAsync<SellerProductElasticDto>(s => s
                    .Index("sellerproduct_index")
                    .RequestConfiguration(r => r.RequestTimeout(TimeSpan.FromSeconds(30))) // VIN araması için timeout 30 saniyeye çıkarıldı
                    .Query(_ => boolQuery)
                    .Sort(st =>
                    {
                        // If there is a search term, _score MUST be primary sort to ensure "Triger Seti" wins over "Triger Kapağı"
                        if (!string.IsNullOrWhiteSpace(filter.Search))
                        {
                            st.Descending("_score");
                        }

                        // Stock as secondary/primary depending on search context
                        st.Script(sc => sc
                            .Type("number")
                            .Descending()
                            .Script(scr => scr.Source("doc.containsKey('Stock') && !doc['Stock'].empty && doc['Stock'].value > 0 ? 1 : 0"))
                        );
                        
                        st.Ascending("SalePrice");
                        return st;
                    })
                    .From((filter.Page - 1) * filter.PageSize)
                    .Size(filter.PageSize > 0 ? filter.PageSize : 50)
                    .Aggregations(a => a
                        .Cardinality("unique_groups", c => c
                            .Field("PartNumber.keyword") // Gruplanmış benzersiz parça sayısı
                            .PrecisionThreshold(40000)
                        )
                    )
                );
                esStopwatch.Stop();
                _logger.LogInformation("[PERF-ES] Elasticsearch sorgusu tamamlandı: {Duration}ms, IsValid: {IsValid}, Dönen kayıt: {Count}, Total: {Total}", 
                    esStopwatch.ElapsedMilliseconds, result.IsValid, result.Documents?.Count() ?? 0, result.Total);
                
                // DEBUG: DatProcessNumbers ile arama yapıldıysa ve sonuç yoksa detaylı log
                if (filter.DatProcessNumbers != null && filter.DatProcessNumbers.Any() && (result.Documents?.Count() ?? 0) == 0)
                {
                    _logger.LogWarning("[DEBUG-DPN] Elasticsearch'ten SONUÇ BULUNAMADI! DatProcessNumber sayısı: {Count}, İlk 10 kod: {Codes}", 
                        filter.DatProcessNumbers.Count,
                        string.Join(", ", filter.DatProcessNumbers.Take(10)));
                    
                    // Elasticsearch debug bilgisi
                    if (!string.IsNullOrEmpty(result.DebugInformation))
                    {
                        _logger.LogWarning("[DEBUG-DPN] Elasticsearch DebugInfo: {DebugInfo}", 
                            result.DebugInformation.Substring(0, Math.Min(500, result.DebugInformation.Length)));
                    }
                }
                // DEBUG: OEM kodları ile arama yapıldıysa ve sonuç yoksa detaylı log
                else if (filter.OemCodes != null && filter.OemCodes.Any() && (result.Documents?.Count() ?? 0) == 0)
                {
                    _logger.LogWarning("[DEBUG-OEM] Elasticsearch'ten SONUÇ BULUNAMADI! OEM kod sayısı: {Count}, İlk 10 kod: {Codes}", 
                        filter.OemCodes.Count,
                        string.Join(", ", filter.OemCodes.Take(10)));
                    
                    // Elasticsearch debug bilgisi
                    if (!string.IsNullOrEmpty(result.DebugInformation))
                    {
                        _logger.LogWarning("[DEBUG-OEM] Elasticsearch DebugInfo: {DebugInfo}", 
                            result.DebugInformation.Substring(0, Math.Min(500, result.DebugInformation.Length)));
                    }
                }

                if (!result.IsValid)
                {
                    _logger.LogError("[PERF-ES] Elasticsearch sorgusu BAŞARISIZ! Exception: {Exception}, DebugInfo: {DebugInfo}", 
                        result.OriginalException?.Message ?? "null", 
                        result.DebugInformation ?? "null");
                    rs.AddError($"Filter query failed: {result.OriginalException?.Message}");
                    return rs;
                }

                // De-duplicate items: If same Seller Name has same PartNumber AND same Manufacturer multiple times, pick Best.
                // Include Manufacturer in Key to preserve brand variants.
                // De-duplicate items: If same Seller Name has same PartNumber AND same Manufacturer multiple times, pick Best.
                // Include Manufacturer in Key to preserve brand variants.
                // Check Global Setting
                var dedupStopwatch = System.Diagnostics.Stopwatch.StartNew();
                List<SellerProductElasticDto> products;
                DeduplicateResult? dedup = null;
                bool shouldGroup = filter.ShouldGroupOems || (metadata != null && metadata.ShouldGroupOems);

                var documents = result.Documents?.ToList() ?? new List<SellerProductElasticDto>();

                if (!shouldGroup)
                {
                    products = documents;
                }
                else
                {
                     dedup = CorrectAndDeduplicate(documents, true);
                     products = dedup.Kept;
                }
                dedupStopwatch.Stop();
                _logger.LogInformation("[PERF] Deduplication süresi: {Duration}ms, Sonuç: {Count} ürün", 
                    dedupStopwatch.ElapsedMilliseconds, products.Count);
                
                // PERFORMANS: VIN aramasında similar count skip - 87s kazanç!
                bool skipSimilarCount = (filter.DatProcessNumbers != null && filter.DatProcessNumbers.Any()) || 
                                       (filter.OemCodes != null && filter.OemCodes.Any());
                _logger.LogInformation("[PERF] VIN araması tespit edildi: {IsVin}, Similar count skip: {Skip}", 
                    skipSimilarCount, skipSimilarCount);
                
                var joinStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var viewModels = await JoinRelatedData(products, skipSimilarCount);
                
                // POPULATE GROUP COUNTS: Use the deduplication group counts if we skipped the expensive DB skipSimilarCount
                if (shouldGroup && dedup != null && dedup.GroupCounts.Any())
                {
                    foreach (var vm in viewModels)
                    {
                        if (dedup.GroupCounts.TryGetValue(vm.SellerItemId.ToString(), out int groupSize))
                        {
                            // In grouping context, SimilarProductCount represents other members of this group
                            vm.SimilarProductCount = groupSize - 1;
                        }
                    }
                }
                
                joinStopwatch.Stop();
                _logger.LogInformation("[PERF] JoinRelatedData süresi: {Duration}ms", joinStopwatch.ElapsedMilliseconds);

                rs.Result = new Paging<List<SellerProductViewModel>>
                {
                    Data = viewModels
                };

                if (shouldGroup)
                {
                    // Set DataCount to the total number of unique part groups found (Cardinality Aggregation)
                    var uniqueCount = result.Aggregations.Cardinality("unique_groups")?.Value ?? 0;
                    rs.Result.DataCount = (int)Math.Max(uniqueCount, viewModels.Count);
                }
                else
                {
                    rs.Result.DataCount = (int)result.Total;
                }
                
                // Keep the TotalRawCount property but fill it with standard total for now
                rs.Result.TotalRawCount = (int)result.Total;
                
                // Add the Data assignment here (it was implicitly separate before)
                rs.Result.Data = viewModels;
                
                totalStopwatch.Stop();
                _logger.LogInformation("[PERF] *** TOPLAM GetByFilterPagingAsync süresi: {Duration}ms ***", 
                    totalStopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                rs.AddError($"GetByFilterPagingAsync error: {e.Message}");
            }

            return rs;
        }

        public async Task<IActionResult<SearchFilterAggregations>> GetSearchAggregationsAsync(SearchFilterReguestDto filter)
        {
            var rs = OperationResult.CreateResult<SearchFilterAggregations>();
            try
            {
                // Fetch Dynamic Metadata
                var metadata = await _synonymService.GetSearchMetadataAsync();

                // Multi-Tenant Isolation
                List<int>? allowedSellerIds = null;
                if (_tenantProvider.IsMultiTenantEnabled)
                {
                    allowedSellerIds = await _sellerService.GetAllSellerIds();
                }

                // Base query includes SearchText, Categories, Brands, Price, Stock
                // BUT EXCLUDES specific Vehicle selections (Manufacturer, Model)
                var baseQuery = BuildBaseQuery(filter, metadata, allowedSellerIds);

                var result = await _client.SearchAsync<SellerProductElasticDto>(s => s
                    .Index("sellerproduct_index")
                    .Size(0) // We only want aggregations
                    .Query(_ => baseQuery)
                    .Aggregations(a => a
                        // 1. Manufacturers (Scoped to Base Query - shows all relevant manufacturers)
                        .Terms("manufacturers", t => t.Field(f => f.ManufacturerName.Suffix("keyword")).Size(1000))
                        
                        // 2. Base Models (Filtered by Selected Manufacturer if any)
                        .Filter("models_filter", f => f
                            .Filter(q => {
                                var musts = new List<QueryContainer>();
                                if(filter.ManufacturerNames != null && filter.ManufacturerNames.Any())
                                    musts.Add(new TermsQuery { Field = "ManufacturerName.keyword", Terms = filter.ManufacturerNames.Cast<object>() });
                                else if (!string.IsNullOrWhiteSpace(filter.SingleManufacturerName))
                                    musts.Add(new TermQuery { Field = "ManufacturerName.keyword", Value = filter.SingleManufacturerName });
                                
                                return musts.Any() ? new BoolQuery { Must = musts } : (QueryContainer)new MatchAllQuery();
                            })
                            .Aggregations(aa => aa
                                .Terms("models", t => t.Field(f => f.BaseModelName.Suffix("keyword")).Size(1000))
                            )
                        )

                        // 3. Sub Models (Filtered by Selected Manufacturer AND Selected Model)
                .Filter("submodels_filter", f => f
                    .Filter(q => {
                        var musts = new List<QueryContainer>();
                        // Check Manufacturers
                        if(filter.ManufacturerNames != null && filter.ManufacturerNames.Any())
                            musts.Add(new TermsQuery { Field = "ManufacturerName.keyword", Terms = filter.ManufacturerNames.Cast<object>() });
                        else if (!string.IsNullOrWhiteSpace(filter.SingleManufacturerName))
                            musts.Add(new TermQuery { Field = "ManufacturerName.keyword", Value = filter.SingleManufacturerName });
                        
                        // Check Models
                        if(filter.BaseModelNames != null && filter.BaseModelNames.Any())
                            musts.Add(new TermsQuery { Field = "BaseModelName.keyword", Terms = filter.BaseModelNames.Cast<object>() });
                        else if (!string.IsNullOrWhiteSpace(filter.SingleModelName))
                            musts.Add(new TermQuery { Field = "BaseModelName.keyword", Value = filter.SingleModelName });
                        
                        return musts.Any() ? new BoolQuery { Must = musts } : (QueryContainer)new MatchAllQuery();
                    })
                    .Aggregations(aa => aa
                        .Nested("submodels_nested", n => n
                            .Path(p => p.SubModelsJson)
                            .Aggregations(aaa => aaa
                                .Terms("submodels", t => t.Field("SubModelsJson.Name.keyword").Size(100))
                            )
                        )
                    )
                )
            )
        );

                if (!result.IsValid)
                {
                    rs.AddError($"Aggregation query failed: {result.OriginalException?.Message}");
                    return rs;
                }

                var aggs = new SearchFilterAggregations();

                // Parse Manufacturers
                if (result.Aggregations.Terms("manufacturers") is { } manufAgg)
                    aggs.Manufacturers = manufAgg.Buckets.Select(b => b.Key).ToList();

                // Parse Models
                var modelFilter = result.Aggregations.Filter("models_filter");
                if (modelFilter?.Terms("models") is { } modelAgg)
                    aggs.BaseModels = modelAgg.Buckets.Select(b => b.Key).ToList();

                // Parse SubModels
                var subFilter = result.Aggregations.Filter("submodels_filter");
                if (subFilter?.Nested("submodels_nested")?.Terms("submodels") is { } subAgg)
                    aggs.SubModels = subAgg.Buckets.Select(b => b.Key).ToList();

                rs.Result = aggs;
            }
            catch (Exception ex)
            {
                rs.AddError($"GetSearchAggregationsAsync error: {ex.Message}");
            }

            return rs;
        }

        private BoolQuery BuildBaseQuery(SearchFilterReguestDto filter, SearchMetadataContainer metadata, List<int>? allowedSellerIds = null)
        {
            var boolQuery = new BoolQuery { Must = new List<QueryContainer>() };
            var filterQueries = new List<QueryContainer>
            {
                new NumericRangeQuery { Field = Infer.Field<SellerProductElasticDto>(f => f.SalePrice), GreaterThan = 0.0 }
            };

            // [Şasi-LOG] Log details for debugging missing results
            _logger.LogInformation("[Şasi-DEBUG] BuildBaseQuery - MultiTenant: {MT}, Sellers: {Sellers}, DPN Count: {DpnCount}", 
                _tenantProvider.IsMultiTenantEnabled, 
                allowedSellerIds != null ? string.Join(",", allowedSellerIds) : "null",
                filter.DatProcessNumbers?.Count ?? 0);

            if (filter.OnlyInStock)
            {
                filterQueries.Add(new NumericRangeQuery { Field = Infer.Field<SellerProductElasticDto>(f => f.Stock), GreaterThan = 0 });
            }

            // Price range filtering
            if (filter.MinPrice.HasValue || filter.MaxPrice.HasValue)
            {
                filterQueries.Add(new NumericRangeQuery
                {
                    Field = Infer.Field<SellerProductElasticDto>(f => f.SalePrice),
                    GreaterThanOrEqualTo = filter.MinPrice,
                    LessThanOrEqualTo = filter.MaxPrice
                });
            }

            // Category filtering
            if (filter.CategoryIds != null && filter.CategoryIds.Any())
            {
                filterQueries.Add(new TermsQuery { Field = Infer.Field<SellerProductElasticDto>(f => f.CategoryId), Terms = filter.CategoryIds.Cast<object>() });
            }

            // Brand filtering
            if (filter.BrandIds != null && filter.BrandIds.Any())
            {
                filterQueries.Add(new TermsQuery { Field = Infer.Field<SellerProductElasticDto>(f => f.BrandId), Terms = filter.BrandIds.Cast<object>() });
            }

            // Product filtering
            if (filter.ProductIds != null && filter.ProductIds.Any())
            {
                filterQueries.Add(new TermsQuery { Field = Infer.Field<SellerProductElasticDto>(f => f.ProductId), Terms = filter.ProductIds.Cast<object>() });
            }

            // DotPartName filtering (sadece DotPartName için - DatProcessNumber aşağıda ayrı işleniyor)
            if (filter.DotPartNames != null && filter.DotPartNames.Any())
            {
                // Exact DotPartName (.keyword üzerinden)
                filterQueries.Add(new TermsQuery 
                { 
                    Field = "DotPartName.keyword", 
                    Terms = filter.DotPartNames.Cast<object>() 
                });
            }

            // Image filtering
            if (filter.OnlyWithImage)
            {
                filterQueries.Add(new ExistsQuery { Field = Infer.Field<SellerProductElasticDto>(f => f.MainImageUrl) });
            }

            // VIN Hybrid Filtering: Combine DatProcessNumbers and OemCodes
            if ((filter.DatProcessNumbers != null && filter.DatProcessNumbers.Any()) || (filter.OemCodes != null && filter.OemCodes.Any()))
            {
                var vinSearchQueries = new List<QueryContainer>();

                // 1. DatProcessNumbers (Exact Match)
                if (filter.DatProcessNumbers != null && filter.DatProcessNumbers.Any())
                {
                    vinSearchQueries.Add(new TermsQuery 
                    { 
                        Field = Infer.Field<SellerProductElasticDto>(f => f.DatProcessNumber), 
                        Terms = filter.DatProcessNumbers.Cast<object>() 
                    });
                }

                // 2. OEM Codes (Exact Match — MatchQuery kaldırıldı, saçma eşleşmeleri önlemek için)
                if (filter.OemCodes != null && filter.OemCodes.Any())
                {
                    // Orijinal ve temizlenmiş versiyonları birlikte kullan
                    var originalOemCodes = filter.OemCodes
                        .Select(oem => oem.Trim().ToUpperInvariant())
                        .Where(oem => !string.IsNullOrEmpty(oem))
                        .Distinct()
                        .ToList();

                    var normalizedOemCodes = filter.OemCodes
                        .Select(oem => oem.Trim().ToUpperInvariant()
                            .Replace(" ", "").Replace("-", "").Replace(".", "").Replace("/", ""))
                        .Where(oem => !string.IsNullOrEmpty(oem))
                        .Distinct()
                        .ToList();

                    // Tüm varyasyonları birleştir (orijinal + temizlenmiş)
                    var allOemVariants = originalOemCodes.Concat(normalizedOemCodes).Distinct().ToList();

                    if (allOemVariants.Any())
                    {
                        var oemSubQueries = new List<QueryContainer>();
                        
                        // Exact match — PartNumber üzerinde (keyword)
                        oemSubQueries.Add(new TermsQuery 
                        { 
                            Field = "PartNumber", 
                            Terms = allOemVariants.Cast<object>() 
                        });

                        // Exact match — OemCode üzerinde (keyword)
                        oemSubQueries.Add(new TermsQuery 
                        { 
                            Field = "OemCode.keyword", 
                            Terms = allOemVariants.Cast<object>() 
                        });

                        // Exact match — Parts.Oem üzerinde (keyword)
                        oemSubQueries.Add(new TermsQuery 
                        { 
                            Field = "Parts.Oem.keyword", 
                            Terms = allOemVariants.Cast<object>() 
                        });

                        vinSearchQueries.Add(new BoolQuery { Should = oemSubQueries, MinimumShouldMatch = 1 });
                    }
                }

                if (vinSearchQueries.Count > 1)
                {
                    filterQueries.Add(new BoolQuery { Should = vinSearchQueries, MinimumShouldMatch = 1 });
                }
                else if (vinSearchQueries.Count == 1)
                {
                    filterQueries.Add(vinSearchQueries[0]);
                }
            }

            // Tenant Isolation
            if (allowedSellerIds != null)
            {
                 if (allowedSellerIds.Any())
                 {
                     filterQueries.Add(new TermsQuery { Field = Infer.Field<SellerProductElasticDto>(f => f.SellerId), Terms = allowedSellerIds.Cast<object>() });
                 }
                 else
                 {
                     filterQueries.Add(new MatchNoneQuery());
                 }
            }

            boolQuery.Filter = filterQueries;

            // Search Text Logic
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var smartQuery = SearchEngineHelper.BuildSmartSearchQuery(filter.Search, metadata);
                
                ((List<QueryContainer>)boolQuery.Must).Add(smartQuery);
            }

            return boolQuery;
        }

        private void ApplyVehicleFilters(BoolQuery boolQuery, SearchFilterReguestDto filter)
        {
            var filters = boolQuery.Filter?.ToList() ?? new List<QueryContainer>();

            // Manufacturer Filter
            if (!string.IsNullOrEmpty(filter.ManufacturerKey))
            {
                var queries = new List<QueryContainer>
                {
                    new TermQuery { Field = "ManufacturerKey", Value = filter.ManufacturerKey }
                };

                if (!string.IsNullOrWhiteSpace(filter.SingleManufacturerName))
                {
                    queries.Add(new TermQuery { Field = "ManufacturerName.keyword", Value = filter.SingleManufacturerName });
                }

                filters.Add(new BoolQuery { Should = queries, MinimumShouldMatch = 1 });
            }
            else if (filter.ManufacturerKeys != null && filter.ManufacturerKeys.Any())
            {
                // [VIN-FIX] Allow multiple ManufacturerKeys OR Null/Empty (for universal/aftermarket parts)
                filters.Add(new BoolQuery 
                { 
                    Should = new List<QueryContainer>
                    {
                        new TermsQuery { Field = "ManufacturerKey", Terms = filter.ManufacturerKeys.Cast<object>() },
                        new BoolQuery { MustNot = new List<QueryContainer> { new ExistsQuery { Field = "ManufacturerKey" } } },
                        new TermQuery { Field = "ManufacturerKey", Value = "" }
                    },
                    MinimumShouldMatch = 1
                });
            }
            else if (filter.ManufacturerNames != null && filter.ManufacturerNames.Any())
            {
                 filters.Add(new TermsQuery { Field = "ManufacturerName.keyword", Terms = filter.ManufacturerNames.Cast<object>() });
            }
            else if (!string.IsNullOrWhiteSpace(filter.SingleManufacturerName))
            {
                filters.Add(new TermQuery { Field = "ManufacturerName.keyword", Value = filter.SingleManufacturerName });
            }

            // Model Filter
            if (!string.IsNullOrEmpty(filter.BaseModelKey))
            {
                var queries = new List<QueryContainer>
                {
                    new TermQuery { Field = "BaseModelKey", Value = filter.BaseModelKey }
                };

                if (!string.IsNullOrWhiteSpace(filter.SingleModelName))
                {
                    queries.Add(new TermQuery { Field = "BaseModelName.keyword", Value = filter.SingleModelName });
                }

                filters.Add(new BoolQuery { Should = queries, MinimumShouldMatch = 1 });
            }
            else if (filter.BaseModelNames != null && filter.BaseModelNames.Any())
            {
                 filters.Add(new TermsQuery { Field = "BaseModelName.keyword", Terms = filter.BaseModelNames.Cast<object>() });
            }
            else if (!string.IsNullOrWhiteSpace(filter.SingleModelName))
            {
                filters.Add(new TermQuery { Field = "BaseModelName.keyword", Value = filter.SingleModelName });
            }

            // SubModel Filter
            if (filter.SubModelKeys != null && filter.SubModelKeys.Any())
            {
                filters.Add(new NestedQuery
                {
                    Path = "SubModelsJson",
                    Query = new TermsQuery
                    {
                        Field = "SubModelsJson.Key.keyword",
                        Terms = filter.SubModelKeys.Cast<object>()
                    }
                });
            }
            else if (filter.SubModelNames != null && filter.SubModelNames.Any())
            {
                filters.Add(new NestedQuery
                {
                    Path = "SubModelsJson",
                    Query = new TermsQuery
                    {
                        Field = "SubModelsJson.Name.keyword",
                        Terms = filter.SubModelNames.Cast<object>()
                    }
                });
            }
            else if (!string.IsNullOrWhiteSpace(filter.SingleSubModelName))
            {
                filters.Add(new NestedQuery
                {
                    Path = "SubModelsJson",
                    Query = new WildcardQuery
                    {
                        Field = "SubModelsJson.Name",
                        Value = $"*{filter.SingleSubModelName}*",
                        CaseInsensitive = true
                    }
                });
            }

            boolQuery.Filter = filters;
        }

        private async Task<List<SellerProductViewModel>> JoinRelatedData(List<SellerProductElasticDto> products, bool skipSimilarCount = false)
        {
            var joinStopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[PERF-JOIN] Başlangıç - Ürün sayısı: {Count}, SkipSimilarCount: {Skip}", 
                products.Count, skipSimilarCount);
            
            var prepStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var brandIds = products.Where(p => p.BrandId.HasValue).Select(p => p.BrandId!.Value).Distinct().ToList();
            var categoryIds = products.Where(p => p.CategoryId.HasValue).Select(p => p.CategoryId!.Value).Distinct().ToList();
            var productIds = products.Select(p => p.ProductId).Distinct().ToList();
            
            // Collect needed (ProductId, OemCodeList, SellerId) triples
            var groupCodeQueries = products
                .Select(p => {
                    var codes = (p.OemCode ?? new List<string>()).ToList();
                    var partNumber = p.PartNumber?.Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(partNumber) && !codes.Contains(partNumber))
                    {
                        codes.Add(partNumber);
                    }
                    return (ProductId: p.ProductId, OemCodes: codes, SellerId: p.SellerId);
                })
                .DistinctBy(x => x.ProductId) 
                .ToList();

            // Parallelize external data fetching
            var productQueries = products.Select(p => (
                ProductId: p.ProductId, 
                OemCodes: p.OemCode ?? new List<string>(), 
                PartNumber: p.PartNumber,
                SellerId: p.SellerId 
            )).ToList();
            prepStopwatch.Stop();
            _logger.LogInformation("[PERF-JOIN] Hazırlık süresi: {Duration}ms - BrandIds: {BrandCount}, CategoryIds: {CategoryCount}, ProductIds: {ProductCount}", 
                prepStopwatch.ElapsedMilliseconds, brandIds.Count, categoryIds.Count, productIds.Count);

            var parallelStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var brandsTask = GetBrandsByIds(brandIds);
            var categoriesTask = GetCategoriesByIds(categoryIds);
            var imagesTask = GetImagesByProductIds(productIds);
            
            // PERFORMANS: VIN aramasında similar count skip - 87s kazanç!
            Dictionary<int, long> similarCountsDict;
            if (skipSimilarCount)
            {
                _logger.LogInformation("[PERF-JOIN] Similar count SKIP edildi (VIN araması)");
                similarCountsDict = new Dictionary<int, long>();
                await Task.WhenAll(brandsTask, categoriesTask, imagesTask);
            }
            else
            {
                _logger.LogInformation("[PERF-JOIN] Similar count hesaplanıyor...");
                var similarCountsTask = GetSimilarProductCountsAsync(productQueries);
                await Task.WhenAll(brandsTask, categoriesTask, imagesTask, similarCountsTask);
                similarCountsDict = await similarCountsTask;
            }
            parallelStopwatch.Stop();
            _logger.LogInformation("[PERF-JOIN] Paralel veri çekme süresi: {Duration}ms", parallelStopwatch.ElapsedMilliseconds);

            var brands = await brandsTask;
            var categories = await categoriesTask;
            var images = await imagesTask;
            _logger.LogInformation("[PERF-JOIN] Çekilen veri - Brands: {BrandCount}, Categories: {CategoryCount}, Images: {ImageCount}", 
                brands.Count, categories.Count, images.Count);

            var brandsDict = brands.ToDictionary(b => b.Id!.Value, b => b);
            var categoriesDict = categories.ToDictionary(c => c.Id!.Value, c => c);
            var imagesDict = images.GroupBy(img => img.ProductId).ToDictionary(g => g.Key, g => g.ToList());

            var mappingStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var viewModels = new List<SellerProductViewModel>();
            foreach (var p in products)
            {
                var originalBrand = p.BrandId.HasValue && brandsDict.ContainsKey(p.BrandId.Value) ? brandsDict[p.BrandId.Value] : null;
                
                // Clone or create brand to avoid mutating shared objects in brandsDict
                BrandDto? brand = null;
                if (originalBrand != null)
                {
                    brand = new BrandDto 
                    { 
                        Id = originalBrand.Id, 
                        Name = originalBrand.Name,
                        Status = originalBrand.Status
                    };
                }

                // Ensure the brand name shown in UI matches the Seller's specific brand (ProductBrandName) from ES
                if (!string.IsNullOrWhiteSpace(p.ProductBrandName))
                {
                    if (brand == null) brand = new BrandDto { Id = p.BrandId ?? 0 };
                    brand.Name = p.ProductBrandName.Trim();
                }

                var vm = new SellerProductViewModel
                {
                    SellerItemId = p.SellerItemId,
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductDescription = p.ProductDescription,
                    ProductBarcode = p.ProductBarcode,
                    DocumentUrl = p.DocumentUrl,
                    MainImageUrl = !string.IsNullOrEmpty(p.MainImageUrl) 
                        ? p.MainImageUrl 
                        : (imagesDict.ContainsKey(p.ProductId) && imagesDict[p.ProductId].Any() 
                            ? imagesDict[p.ProductId].First().FileName 
                            : null),
                    Stock = (int)p.Stock,
                    SalePrice = p.SalePrice,
                    CostPrice = p.CostPrice,
                    Currency = p.Currency,
                    Unit = p.Unit,
                    SellerId = p.SellerId,
                    SellerName = p.SellerName,
                    SellerModifiedDate = p.SellerModifiedDate,
                    SourceId = p.SourceId,
                    Step = p.Step,
                    MinSaleAmount = p.MinSaleAmount,
                    MaxSaleAmount = p.MaxSaleAmount,

                    PartNumber = !string.IsNullOrWhiteSpace(p.PartNumber) ? p.PartNumber : (p.OemCode != null && p.OemCode.Any() ? p.OemCode.First() : "-"), 
                    IsEquivalent = string.IsNullOrWhiteSpace(p.PartNumber) && p.OemCode != null && p.OemCode.Any(),
                    DotPartName = p.DotPartName,
                    ManufacturerName = p.ManufacturerName, 
                    VehicleTypeName = p.VehicleTypeName,
                    DotPartDescription = p.DotPartDescription,
                    BaseModelName = p.BaseModelName,
                    NetPrice = p.NetPrice,
                    PriceDate = p.PriceDate,
                    DatProcessNumber = p.DatProcessNumber,
                    VehicleType = p.VehicleType,
                    ManufacturerKey = p.ManufacturerKey,
                    BaseModelKey = p.BaseModelKey,
                    OemCode = p.OemCode, 
                    SubModelsJson = p.SubModelsJson,
                    Parts = p.Parts,
                    Brand = brand,
                    Categories = p.CategoryId.HasValue && categoriesDict.ContainsKey(p.CategoryId.Value) 
                        ? new List<CategoryDto> { categoriesDict[p.CategoryId.Value] } 
                        : new List<CategoryDto>(),
                    Images = imagesDict.ContainsKey(p.ProductId) ? imagesDict[p.ProductId].Select(img => new ProductImageDto 
                    { 
                        Id = img.Id, 
                        ProductId = img.ProductId, 
                        FileName = img.FileName, 
                        FileGuid = img.FileGuid 
                    }).ToList() : new List<ProductImageDto>()
                };

                // Remove display PartNumber from OemList to avoid duplication
                if (vm.OemCode != null && !string.IsNullOrEmpty(vm.PartNumber))
                {
                    vm.OemCode = vm.OemCode.Where(x => !string.Equals(x, vm.PartNumber, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Calculate SimilarProductCount
                if (similarCountsDict.TryGetValue(p.ProductId, out var totalSimilar))
                {
                    // totalSimilar is total hits for these codes.
                    // We subtract 1 to exclude the current item if it was found.
                    vm.SimilarProductCount = (int)(totalSimilar > 0 ? totalSimilar - 1 : 0);
                }
                else
                {
                    vm.SimilarProductCount = 0;
                }
                viewModels.Add(vm);
            }
            mappingStopwatch.Stop();
            _logger.LogInformation("[PERF-JOIN] ViewModel mapping süresi: {Duration}ms", mappingStopwatch.ElapsedMilliseconds);
            
            joinStopwatch.Stop();
            _logger.LogInformation("[PERF-JOIN] *** TOPLAM JoinRelatedData süresi: {Duration}ms ***", joinStopwatch.ElapsedMilliseconds);
            
            return viewModels;
        }

        private async Task<List<BrandDto>> GetBrandsByIds(List<int> brandIds)
        {
            if (!brandIds.Any()) return new List<BrandDto>();
            var response = await _client.SearchAsync<BrandDto>(s => s
                .Index("brand_index")
                .Size(brandIds.Count)
                .Query(q => q.Terms(t => t.Field("Id").Terms(brandIds)))
            );
            return response.IsValid ? response.Documents.ToList() : new List<BrandDto>();
        }

        private async Task<List<CategoryDto>> GetCategoriesByIds(List<int> categoryIds)
        {
            if (!categoryIds.Any()) return new List<CategoryDto>();
            
            var response = await _client.SearchAsync<CategoryDto>(s => s
                .Index("category_index")
                .Size(categoryIds.Count)
                .Query(q => q.Terms(t => t.Field("Id").Terms(categoryIds)))
            );
            
            return response.IsValid ? response.Documents.ToList() : new List<CategoryDto>();
        }

        private async Task<List<ImageIndexDto>> GetImagesByProductIds(List<int> productIds)
        {
            if (!productIds.Any()) return new List<ImageIndexDto>();
            var response = await _client.SearchAsync<ImageIndexDto>(s => s
                .Index("image_index")
                .Size(productIds.Count * 2)
                .Query(q => q.Terms(t => t.Field(f => f.ProductId).Terms(productIds)))
            );
            return response.IsValid ? response.Documents.ToList() : new List<ImageIndexDto>();
        }

        private DeduplicateResult CorrectAndDeduplicate(List<SellerProductElasticDto> documents, bool shouldGroupOems = false)
        {
            var result = new DeduplicateResult();
            if (shouldGroupOems)
            {
                // Aggressive transitive deduplication for each seller separately
                var sellerGroups = documents.GroupBy(p => p.SellerId);
                var finalKept = new List<SellerProductElasticDto>();

                foreach (var sellerGroup in sellerGroups)
                {
                    var items = sellerGroup.ToList();
                    var groups = new List<List<SellerProductElasticDto>>();

                    foreach (var item in items)
                    {
                        var itemCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (!string.IsNullOrEmpty(item.PartNumber)) 
                        {
                            foreach(var t in ParseGroupCodes(item.PartNumber)) itemCodes.Add(t);
                        }
                        if (item.OemCode != null) 
                        {
                            foreach(var c in item.OemCode) 
                            {
                                if(!string.IsNullOrEmpty(c)) 
                                {
                                    foreach(var t in ParseGroupCodes(c)) itemCodes.Add(t);
                                }
                            }
                        }

                        // Find all existing groups that share any code with this item
                        var matchingGroups = groups.Where(g => g.Any(existingItem => {
                            var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            if (!string.IsNullOrEmpty(existingItem.PartNumber)) 
                            {
                                foreach(var t in ParseGroupCodes(existingItem.PartNumber)) existingCodes.Add(t);
                            }
                            if (existingItem.OemCode != null) 
                            {
                                foreach(var c in existingItem.OemCode) 
                                {
                                    if(!string.IsNullOrEmpty(c)) 
                                    {
                                        foreach(var t in ParseGroupCodes(c)) existingCodes.Add(t);
                                    }
                                }
                            }
                            return itemCodes.Overlaps(existingCodes);
                        })).ToList();

                        if (!matchingGroups.Any())
                        {
                            groups.Add(new List<SellerProductElasticDto> { item });
                        }
                        else if (matchingGroups.Count == 1)
                        {
                            matchingGroups[0].Add(item);
                        }
                        else
                        {
                            // Merge multiple groups
                            var mergedGroup = matchingGroups.SelectMany(g => g).ToList();
                            mergedGroup.Add(item);
                            foreach (var g in matchingGroups) groups.Remove(g);
                            groups.Add(mergedGroup);
                        }
                    }

                    // Pick the best item for each group
                    foreach (var g in groups)
                    {
                        var best = g
                            .OrderByDescending(x => x.Stock > 0)
                            .ThenByDescending(x => x.SellerItemId)
                            .First();
                        
                        // Grubun tüm OEM kodlarını birleştirip best item'a ekle
                        // Böylece muadil modal'ı açıldığında tüm kodlarla arama yapılabilir
                        if (g.Count > 1)
                        {
                            var allGroupCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var member in g)
                            {
                                if (member.OemCode != null)
                                {
                                    foreach (var code in member.OemCode)
                                    {
                                        if (!string.IsNullOrWhiteSpace(code)) allGroupCodes.Add(code);
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(member.PartNumber))
                                {
                                    allGroupCodes.Add(member.PartNumber);
                                }
                            }
                            best.OemCode = allGroupCodes.ToList();
                        }
                        
                        finalKept.Add(best);
                        // Store the total count of hidden items in this group
                        if (g.Count > 1)
                        {
                            // Use SellerItemId as key for precise mapping
                            result.GroupCounts[best.SellerItemId.ToString()] = g.Count;
                        }
                    }
                }

                result.Kept = finalKept;
                return result;
            }

            // Normal deduplication: Keep different brands/names separate
            result.Kept = documents
                .GroupBy(p => new { 
                    PartKey = p.PartNumber?.Trim().ToUpperInvariant() ?? "-",
                    SellerId = p.SellerId,
                    BrandKey = p.ProductBrandName?.Trim().ToUpperInvariant() ?? "-",
                    Price = p.SalePrice,
                    NameKey = p.ProductName?.Trim().ToUpperInvariant() ?? "-"
                })
                .Select(g => g
                    .OrderByDescending(x => x.Stock > 0)
                    .ThenByDescending(x => x.SellerItemId) 
                    .First())
                .ToList();

            return result;
        }

        private static List<int>? _productIdsWithImagesCache = null;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        public async Task<IActionResult<List<SellerProductViewModel>>> GetSimilarProductsAsync(string oemCode)
        {
            return await GetSimilarProductsAsync(new List<string> { oemCode });
        }

        public async Task<IActionResult<List<SellerProductViewModel>>> GetSimilarProductsAsync(List<string> oemCodes)
        {
            var rs = OperationResult.CreateResult<List<SellerProductViewModel>>();
            try
            {
                // Fetch Metadata for grouping setting
                var metadata = await _synonymService.GetSearchMetadataAsync();

                if (oemCodes == null || !oemCodes.Any())
                {
                    rs.Result = new List<SellerProductViewModel>();
                    return rs;
                }

                // 1. Gather all tokens from all OemCode strings
                var allTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var codeStr in oemCodes)
                {
                     var tokens = ParseGroupCodes(codeStr);
                     foreach(var t in tokens) allTokens.Add(t);
                }

                if (allTokens.Count == 0)
                {
                    rs.Result = new List<SellerProductViewModel>();
                    return rs;
                }

                var tokensList = allTokens.Select(t => t.ToLowerInvariant()).ToList();
                
                var response = await _client.SearchAsync<SellerProductElasticDto>(s => s
                    .Index("sellerproduct_index")
                    .Size(50)
                    .Query(q => q
                        .Bool(b => b
                            .Filter(f => f.Range(r => r.Field(result => result.SalePrice).GreaterThan(0)))
                            .Should(tokensList.SelectMany(code => new[]
                            {
                                // Search in OemCode
                                (QueryContainer)new TermsQuery 
                                { 
                                    Field = "OemCode", 
                                    Terms = new[] { code }
                                },
                                (QueryContainer)new MatchQuery 
                                { 
                                    Field = "OemCode", 
                                    Query = code
                                },
                                // Search in PartNumber
                                (QueryContainer)new TermsQuery 
                                { 
                                    Field = "PartNumber", 
                                    Terms = new[] { code }
                                },
                                (QueryContainer)new MatchQuery 
                                { 
                                    Field = "PartNumber", 
                                    Query = code
                                }
                            }).ToArray())
                            .MinimumShouldMatch(1)
                        )
                    )
                );

                if (!response.IsValid)
                {
                    rs.AddError("Search failed.");
                    return rs;
                }

                // Muadil ürünler modal'ında gruplama yapma — tüm alternatifleri göster
                // Count (GetSimilarProductCountsAsync) ile tutarlı olması için
                var products = response.Documents.ToList();
                
                var viewModels = await JoinRelatedData(products); 
                // Stokta olan ürünleri öne al, kendi içlerinde ES alaka skorunu koru
                rs.Result = viewModels.OrderByDescending(p => p.Stock > 0).ToList();
            }
            catch (Exception ex)
            {
                rs.AddError(ex.Message);
            }
            return rs;
        }

        private async Task<Dictionary<int, long>> GetSimilarProductCountsAsync(List<(int ProductId, List<string> OemCodes, string? PartNumber, int SellerId)> productQueries)
        {
            var result = new Dictionary<int, long>();
            if (!productQueries.Any()) return result;

            try
            {
                // Chunk the work into batches of 100 to prevent ES timeout / large request body
                const int batchSize = 100;
                for (int i = 0; i < productQueries.Count; i += batchSize)
                {
                    var batch = productQueries.Skip(i).Take(batchSize).ToList();
                    var multiSearchDescriptor = new MultiSearchDescriptor();
                    // Map: requestIndex -> ProductId
                    var indexToProductId = new Dictionary<string, int>(); 
                    int index = 0;

                    foreach (var item in batch)
                    {
                        if (item.OemCodes == null || !item.OemCodes.Any()) continue;

                        // Parse ALL tokens from ALL OemCode strings for this product
                        var allTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        
                        if (!string.IsNullOrEmpty(item.PartNumber))
                        {
                            foreach(var t in ParseGroupCodes(item.PartNumber)) allTokens.Add(t);
                        }

                        foreach (var codeStr in item.OemCodes)
                        {
                             foreach(var t in ParseGroupCodes(codeStr)) allTokens.Add(t);
                        }
                        
                        if (allTokens.Count == 0) continue;
                        
                        var searchKey = index.ToString();
                        indexToProductId[searchKey] = item.ProductId;
                        index++;
                        
                        multiSearchDescriptor.Search<SellerProductElasticDto>(searchKey, s => s
                            .Index("sellerproduct_index")
                            .Size(0)
                            .TrackTotalHits(true)
                            .RequestConfiguration(r => r.RequestTimeout(TimeSpan.FromSeconds(2)))
                            .Query(q => q
                                .Bool(b => b
                                    .Filter(f => f.Range(r => r.Field(res => res.SalePrice).GreaterThan(0)))
                                    .Must(m => m.Bool(mb => mb
                                        .Should(
                                            sh => sh.Terms(t => t.Field("OemCode").Terms(allTokens)),
                                            sh => sh.Terms(t => t.Field("PartNumber").Terms(allTokens))
                                        )
                                        .MinimumShouldMatch(1)
                                    ))
                                )
                            )
                        );
                    }

                    if (!indexToProductId.Any()) continue;

                    var multiResponse = await _client.MultiSearchAsync(multiSearchDescriptor);

                    if (multiResponse.IsValid)
                    {
                        int responseIndex = 0;
                        foreach (var responseItem in multiResponse.AllResponses)
                        {
                            var key = responseIndex.ToString();
                            if (indexToProductId.TryGetValue(key, out var pId))
                            {
                                if (responseItem.ApiCall.Success && responseItem is ISearchResponse<SellerProductElasticDto> searchResponse)
                                {
                                    result[pId] = searchResponse.Total;
                                }
                            }
                            responseIndex++;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // fail silently
            }
            return result;
        }

        private static List<string> ParseGroupCodes(string? rawGroupCodes)
        {
            var result = new List<string>();
            if(string.IsNullOrWhiteSpace(rawGroupCodes)){
                return result;
            }

            var separators = new[] { '|', ',', ';', '-', ' ' };
            var tokens = rawGroupCodes.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach(var token in tokens){
                var trimmedToken = token?.Trim();
                
                if (string.IsNullOrWhiteSpace(trimmedToken)) continue;

                // Normalize: remove dots for better transitive matching (e.g. 960118.12 -> 96011812)
                var normalizedToken = trimmedToken.Replace(".", "");

                if (normalizedToken.Length < 4)
                {
                    continue;
                }

                // Skip if no digits are present
                bool hasDigit = false;
                foreach (char c in normalizedToken)
                {
                    if (char.IsDigit(c))
                    {
                        hasDigit = true;
                        break;
                    }
                }

                if (!hasDigit)
                {
                    continue;
                }
                
                if(seen.Add(normalizedToken)){
                    result.Add(normalizedToken);
                }
            }
            return result;
        }

        private async Task<List<int>> GetProductIdsSubsetFromImageIndex()
        {
            // Simple memory cache to improve performance
            if (_productIdsWithImagesCache != null && (DateTime.Now - _lastCacheUpdate) < CacheDuration)
            {
                return _productIdsWithImagesCache;
            }

            // We fetch up to 10000 product IDs that have images to use in the terms filter
            var response = await _client.SearchAsync<ImageIndexDto>(s => s
                .Index("image_index")
                .Size(10000)
                .Source(sr => sr.Includes(f => f.Field(ff => ff.ProductId)))
            );
            
            if (response.IsValid)
            {
                _productIdsWithImagesCache = response.Documents.Select(d => d.ProductId).Distinct().ToList();
                _lastCacheUpdate = DateTime.Now;
                return _productIdsWithImagesCache;
            }

            return _productIdsWithImagesCache ?? new List<int>();
        }
    }
}
