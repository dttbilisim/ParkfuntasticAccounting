using System;
using ecommerce.Core.Entities;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Domain.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Domain.Shared.Helpers;
using Nest;

namespace ecommerce.Web.Domain.Services.Concreate;

public class SellerProductService : ISellerProductService
{
    private readonly IElasticSearchService _elasticSearchService;
    private readonly IUserCarService _userCarService;
    private readonly ISearchSynonymService _synonymService;

    public SellerProductService(
        IElasticSearchService elasticSearchService, 
        IUserCarService userCarService,
        ISearchSynonymService synonymService)
    {
        _elasticSearchService = elasticSearchService;
        _userCarService = userCarService;
        _synonymService = synonymService;
    }


    public async Task<IActionResult<List<SellerProductViewModel>>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var rs = OperationResult.CreateResult<List<SellerProductViewModel>>();
        try
        {
            // 1. sellerproduct_index'ten ürünleri çek
            var productsResponse = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Query(q => q.Bool(b => b
                    .Filter(f => f.Range(r => r.Field(fld => fld.SalePrice).GreaterThan(0)),
                            f => f.Exists(e => e.Field(fld => fld.SourceId)))
                ))
                .Sort(ss => ss.Descending(p => p.SellerModifiedDate))
                .From((page - 1) * pageSize)
                .Size(pageSize)
            );

            if (!productsResponse.IsValid)
            {
                rs.AddError($"SellerProduct query failed: {productsResponse.OriginalException?.Message}");
                return rs;
            }

            var products = productsResponse.Documents.ToList();
            if (!products.Any())
            {
                rs.Result = new List<SellerProductViewModel>();
                return rs;
            }

            // 2. Brand, Category, Image join
            var viewModels = await JoinRelatedData(products);
            rs.Result = viewModels;
        }
        catch (Exception e)
        {
            rs.AddError($"GetAllAsync error: {e.Message}");
        }

        return rs;
    }

    /// <summary>
    /// Filter ve paging ile ürün listesi
    /// </summary>
    public async Task<IActionResult<Paging<List<SellerProductViewModel>>>> GetByFilterPagingAsync(SearchFilterReguestDto filter)
    {
        var rs = OperationResult.CreateResult<Paging<List<SellerProductViewModel>>>();
        try
        {
            var boolQuery = new BoolQuery { Must = new List<QueryContainer>() };
            var filterQueries = new List<QueryContainer>
            {
                new NumericRangeQuery
                {
                    Field = "SalePrice",
                    GreaterThan = 0.0
                },
                new ExistsQuery
                {
                    Field = "SourceId"
                }
            };
            if (filter.OnlyInStock)
            {
                filterQueries.Add(new NumericRangeQuery
                {
                    Field = "Stock",
                    GreaterThan = 0.0
                });
            }
            boolQuery.Filter = filterQueries;
            List<CarCompatibilityInfo>? compatibilityInfos = null;
            QueryContainer? searchQueryContainer = null;
            var allowedSubModelKeys = BuildAllowedSubModelKeySet(filter.SubModelKeys);
            
            // Fetch metadata early to use for PartNumber and Search queries
            var metadata = await _synonymService.GetSearchMetadataAsync();

            if (filter.OnlyPerfectCompatibility)
            {
                compatibilityInfos = await GetUserCompatibilityInfosAsync();
                if (compatibilityInfos == null || compatibilityInfos.Count == 0)
                {
                    // Kullanıcının garajında araç yoksa, perfect compatibility filtresini görmezden gel
                    // ve normal aramaya devam et
                    filter.OnlyPerfectCompatibility = false;
                    compatibilityInfos = null;
                }
                else if (allowedSubModelKeys != null && allowedSubModelKeys.Count > 0)
                {
                    compatibilityInfos = FilterCompatibilityInfos(compatibilityInfos, allowedSubModelKeys);
                    if (compatibilityInfos.Count == 0)
                    {
                        // Filtrelenen SubModel'ler ile uyumlu araç yoksa da filtreyi devre dışı bırak
                        filter.OnlyPerfectCompatibility = false;
                        compatibilityInfos = null;
                    }
                }
            }

            // CategoryId filter
            if (filter.CategoryId.HasValue)
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new TermQuery
                {
                    Field = "CategoryId",
                    Value = filter.CategoryId.Value
                });
            }

            // Multiple CategoryIds
            if (filter.CategoryIds?.Any() == true)
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new TermsQuery
                {
                    Field = "CategoryId",
                    Terms = filter.CategoryIds.Cast<object>()
                });
            }

            // BrandId filter
            if (filter.BrandId.HasValue)
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new TermQuery
                {
                    Field = "BrandId",
                    Value = filter.BrandId.Value
                });
            }

            // Multiple BrandIds
            if (filter.BrandIds?.Any() == true)
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new TermsQuery
                {
                    Field = "BrandId",
                    Terms = filter.BrandIds.Cast<object>()
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.ManufacturerKey))
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new TermQuery
                {
                    Field = "ManufacturerKey",
                    Value = NormalizeKey(filter.ManufacturerKey)
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.BaseModelKey))
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new TermQuery
                {
                    Field = "BaseModelKey",
                    Value = NormalizeKey(filter.BaseModelKey)
                });
            }

        if (!string.IsNullOrWhiteSpace(filter.PartNumber))
        {
            var normalizedPart = filter.PartNumber.Trim();
            ((List<QueryContainer>)boolQuery.Must).Add(new BoolQuery
            {
                Should = new List<QueryContainer>
                {
                    // Exact match
                    new TermQuery { Field = "PartNumber", Value = normalizedPart, Boost = metadata.Boosts.PartNumberTerm },
                    new TermQuery { Field = "OemCode", Value = normalizedPart, Boost = metadata.Boosts.OemCodeTerm },
                    new TermQuery { Field = "ProductBarcode", Value = normalizedPart, Boost = metadata.Boosts.ProductNameKeyword }, // Assuming Keyword boost for barcode
                    
                    // Match query (case-insensitive, more flexible)
                    new MatchQuery { Field = "PartNumber", Query = normalizedPart, Boost = metadata.Boosts.PartNumberMatch },
                    new MatchQuery { Field = "OemCode", Query = normalizedPart, Boost = metadata.Boosts.OemCodeMatch },
                    new MatchQuery { Field = "ProductBarcode", Query = normalizedPart, Boost = metadata.Boosts.ProductNameMatch }, // Assuming Match boost for barcode

                    // Wildcard (partial match)
                    new WildcardQuery { Field = "OemCode", Value = $"*{normalizedPart}*", CaseInsensitive = true, Boost = metadata.Boosts.OemCodeWildcard },
                    new WildcardQuery { Field = "PartNumber", Value = $"*{normalizedPart}*", CaseInsensitive = true, Boost = metadata.Boosts.PartNumberWildcard },
                    new WildcardQuery { Field = "ProductBarcode", Value = $"*{normalizedPart}*", CaseInsensitive = true, Boost = metadata.Boosts.ProductNameMatch } // Reuse ProductNameMatch or create new setting if needed
                },
                MinimumShouldMatch = 1
            });
        }

            if (filter.DatProcessNumbers?.Any() == true)
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new TermsQuery
                {
                    Field = "DatProcessNumber",
                    Terms = filter.DatProcessNumbers.Cast<object>()
                });
            }

            if (filter.DotPartNames?.Any() == true)
            {
                // Use .keyword for exact filtering logic ("Full Match") 
                // instead of full-text searching which tokenizes "AKS RULMANI" -> matches "TEKER RULMANI"
                ((List<QueryContainer>)boolQuery.Must).Add(new TermsQuery
                {
                    Field = "DotPartName.keyword",
                    Terms = filter.DotPartNames.Cast<object>()
                });
            }

            if (filter.ManufacturerNames?.Any() == true)
            {
                Console.WriteLine($"🏭 Filtering by ManufacturerNames: {string.Join(", ", filter.ManufacturerNames)}");
                ((List<QueryContainer>)boolQuery.Must).Add(new BoolQuery
                {
                    Should = filter.ManufacturerNames.Select(name => (QueryContainer)new MatchQuery
                    {
                        Field = "ManufacturerName",
                        Query = name,
                        Operator = Operator.And
                    }).ToList(),
                    MinimumShouldMatch = 1
                });
            }

            if (filter.BaseModelNames?.Any() == true)
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new BoolQuery
                {
                    Should = filter.BaseModelNames.Select(name => (QueryContainer)new MatchQuery
                    {
                        Field = "BaseModelName",
                        Query = name,
                        Operator = Operator.And
                    }).ToList(),
                    MinimumShouldMatch = 1
                });
            }

            // SubModels nested filter (Alt Model)
            if (filter.SubModelKeys?.Any() == true)
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new NestedQuery
                {
                    Path = "SubModelsJson",
                    Query = new BoolQuery
                    {
                        Should = new List<QueryContainer>
                        {
                            new TermsQuery { Field = "SubModelsJson.Key", Terms = filter.SubModelKeys.Select(NormalizeKey).Cast<object>() },
                            new TermsQuery { Field = "SubModelsJson.Key.keyword", Terms = filter.SubModelKeys.Cast<object>() }
                        },
                        MinimumShouldMatch = 1
                    }
                });
            }
            else if (filter.SubModelNames?.Any() == true)
            {
                ((List<QueryContainer>)boolQuery.Must).Add(new NestedQuery
                {
                    Path = "SubModelsJson",
                    Query = new BoolQuery
                    {
                        Should = filter.SubModelNames.Select(name => (QueryContainer)new MatchQuery
                        {
                            Field = "SubModelsJson.Name",
                            Query = name,
                            Operator = Operator.And
                        }).ToList(),
                        MinimumShouldMatch = 1
                    }
                });
            }

            // Search query (ProductName, ManufacturerName, PartNumber)
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                // Metadata already fetched above
                var processedSearch = SearchEngineHelper.ProcessRomanNumerals(filter.Search, metadata.RomanNumerals, metadata.TechnicalVTerms);
                var searchWords = processedSearch.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var synonymDict = metadata.Synonyms;

                // 3. Build Token-based queries
                foreach (var originalWord in searchWords)
                {
                    var termsForThisWord = new List<string> { originalWord };
                    if (synonymDict.TryGetValue(originalWord, out var synonyms))
                    {
                        termsForThisWord.AddRange(synonyms);
                    }
                    termsForThisWord = termsForThisWord.Distinct().ToList();

                    var wordShouldQueries = new List<QueryContainer>();
                    foreach (var term in termsForThisWord)
                    {
                        var smartQuery = SearchEngineHelper.BuildSmartSearchQuery(term, metadata);
                        wordShouldQueries.Add(smartQuery);
                    }

                    ((List<QueryContainer>)boolQuery.Must).Add(new BoolQuery
                    {
                        Should = wordShouldQueries,
                        MinimumShouldMatch = 1
                    });
                }
            }

            var sortFunc = ResolveSort(filter.Sort);

            if (filter.OnlyPerfectCompatibility)
            {
                var compatibilityResult = await GetPerfectCompatibilityPage(filter, boolQuery, sortFunc, compatibilityInfos!, allowedSubModelKeys);
                rs.Result = compatibilityResult;
                return rs;
            }

            QueryContainer finalQuery;
            var hasMust = boolQuery.Must != null && boolQuery.Must.Any();
            var hasShould = boolQuery.Should != null && boolQuery.Should.Any();
            var hasFilter = boolQuery.Filter != null && boolQuery.Filter.Any();

            if (hasMust || hasShould || hasFilter)
            {
                finalQuery = boolQuery;
            }
            else
            {
                finalQuery = new MatchAllQuery();
            }

            var result = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Query(_ => finalQuery)
                .Sort(sortFunc)
                .From((filter.Page - 1) * filter.PageSize)
                .Size(filter.PageSize)
            );

            if (!result.IsValid)
            {
                rs.AddError($"Filter query failed: {result.OriginalException?.Message}");
                return rs;
            }
            
            
            var products = result.Documents.ToList();
            Console.WriteLine($"📦 Elasticsearch returned {products.Count} products (Total: {result.Total})");
            
            var viewModels = products.Any() ? await JoinRelatedData(products, compatibilityInfos, allowedSubModelKeys) : new List<SellerProductViewModel>();

            if (filter.OnlyPerfectCompatibility)
            {
                viewModels = viewModels
                    .Where(vm => vm.PerfectCompatibilityCars?.Any() == true)
                    .ToList();
            }

            rs.Result = new Paging<List<SellerProductViewModel>>
            {
                Data = viewModels,
                DataCount = filter.OnlyPerfectCompatibility ? viewModels.Count : (int)result.Total
            };

            // BACKEND FALLBACK LOGIC: If strict vehicle filter yielded 0 results, retry with text search
            var hasVehicleFilter = !string.IsNullOrWhiteSpace(filter.ManufacturerKey) || 
                                   !string.IsNullOrWhiteSpace(filter.BaseModelKey) || 
                                   filter.ModelId.HasValue || 
                                   (filter.SubModelKeys != null && filter.SubModelKeys.Any());

            /* 
            // DISABLE FALLBACK LOGIC - It causes wrong brand results (e.g. Mercedes parts for BMW search)
            if (hasVehicleFilter && rs.Result.DataCount == 0)
            {
                // Only retry if we have Names to append or if there was an original search query
                var hasNames = !string.IsNullOrWhiteSpace(filter.SingleManufacturerName) || !string.IsNullOrWhiteSpace(filter.SingleModelName);
                
                if (hasNames || !string.IsNullOrWhiteSpace(filter.Search))
                {
                     // 1. Construct new text query
                     var searchTerms = new List<string>();
                     if(!string.IsNullOrWhiteSpace(filter.Search)) searchTerms.Add(filter.Search);
                     if(!string.IsNullOrWhiteSpace(filter.SingleManufacturerName)) searchTerms.Add(filter.SingleManufacturerName);
                     
                     if(!string.IsNullOrWhiteSpace(filter.SingleModelName))
                     {
                         // Smart Logic: Extract base model name BEFORE parentheses
                         // Example: "Egea (357) HB / CROSS (2016->)" -> "Egea"
                         // NOT the chassis code inside parentheses
                         var mName = filter.SingleModelName;
                         var openParen = mName.IndexOf('(');
                         
                         if(openParen > 0)
                         {
                             // Take the part BEFORE the first parenthesis
                             var baseName = mName.Substring(0, openParen).Trim();
                             if(!string.IsNullOrWhiteSpace(baseName))
                             {
                                 searchTerms.Add(baseName);
                             }
                         }
                         else
                         {
                             searchTerms.Add(mName);
                         }
                     }
                     
                     // Skip SubModelName in fallback - usually too specific
                     // if(!string.IsNullOrWhiteSpace(filter.SingleSubModelName)) searchTerms.Add(filter.SingleSubModelName);
                     
                     var newQuery = string.Join(" ", searchTerms);
                     
                     // 2. Clone filter and CLEAR strict vehicle filters
                     var fallbackFilter = new SearchFilterReguestDto
                     {
                         // Basic pagination/sorting
                         Page = 1, // Reset to page 1 for fallback? Or keep page? Usually reset.
                         PageSize = filter.PageSize,
                         Sort = filter.Sort,
                         
                         // New Broad Search
                         Search = newQuery,
                         
                         // Maintain Category/Brand filters? 
                         // Usually yes, we only want to drop the specific Vehicle ID constraint that failed.
                         CategoryId = filter.CategoryId,
                         CategoryIds = filter.CategoryIds,
                         BrandId = filter.BrandId,
                         BrandIds = filter.BrandIds,
                         
                         // CLEAR STRICT FILTERS
                         ManufacturerKey = null,
                         BaseModelKey = null,
                         ModelId = null,
                         SubModelKeys = null,
                         SubModelNames = null, // Clear strict submodel name matching too
                         
                         // Keep loose filters if necessary, or clear them?
                         // ManufacturerNames list is usually for checkboxes. We might want to keep them if they are separate from the vehicle selector.
                         // But if they are derived from the vehicle selector, we should clear them.
                         // Assuming they are separate filters, we keep them.
                         ManufacturerNames = filter.ManufacturerNames,
                         BaseModelNames = filter.BaseModelNames,
                         DotPartNames = filter.DotPartNames,
                         
                         // Disable Perfect Compatibility for fallback
                         OnlyPerfectCompatibility = false,
                         OnlyInStock = filter.OnlyInStock,
                         
                         // Pass names just in case (though not used for strict filter anymore)
                         SingleManufacturerName = filter.SingleManufacturerName,
                         SingleModelName = filter.SingleModelName,
                         SingleSubModelName = filter.SingleSubModelName
                     };
                     
                     return await GetByFilterPagingAsync(fallbackFilter);
                }
            } 
            */
        }
        catch (Exception e)
        {
            Console.WriteLine($"⚠️ GetByFilterPagingAsync hatası: {e.Message}");
            rs.AddError($"GetByFilterPagingAsync error: {e.Message}");
        }

        return rs;
    }

    /// <summary>
    /// Application-side join: Brand, Category, Image index'lerden veri çek
    /// </summary>
    private async Task<List<SellerProductViewModel>> JoinRelatedData(
        List<SellerProductElasticDto> products,
        List<CarCompatibilityInfo>? compatibilityInfos = null,
        HashSet<string>? allowedSubModelKeys = null)
    {
        // 1. BrandId'leri topla
        var brandIds = products.Where(p => p.BrandId.HasValue).Select(p => p.BrandId!.Value).Distinct().ToList();

        // 2. ProductId'leri topla
        var productIds = products.Select(p => p.ProductId).Distinct().ToList();

        // 3. Parallel queries
        var brandsTask = GetBrandsByIds(brandIds);
        var imagesTask = GetImagesByProductIds(productIds);

        await Task.WhenAll(brandsTask, imagesTask);

        var brandsDict = brandsTask.Result.ToDictionary(b => b.Id!.Value, b => b);
        var imagesDict = imagesTask.Result.GroupBy(img => img.ProductId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        // 4. ViewModel'leri oluştur
        var viewModels = products.Select(p => new SellerProductViewModel
        {
            SellerItemId = p.SellerItemId,
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            ProductDescription = p.ProductDescription,
            ProductBarcode = p.ProductBarcode,
            DocumentUrl = p.DocumentUrl,
            MainImageUrl = p.MainImageUrl,
            Stock = (int)p.Stock,  // Elasticsearch'ten decimal geldiği için int'e cast et
            SalePrice = p.SalePrice,
            CostPrice = p.CostPrice,
            Currency = p.Currency,
            Unit = p.Unit,
            SellerId = p.SellerId,
            SellerName = p.SellerName,
            SellerModifiedDate = p.SellerModifiedDate,

            // DotParts (direkt Elasticsearch'ten geliyor - sadece İLK GroupCode)
            PartNumber = !string.IsNullOrWhiteSpace(p.PartNumber) ? p.PartNumber : (p.OemCode != null && p.OemCode.Any() ? p.OemCode.First() : "-"),
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

            // Joined from other indices
            Brand = p.BrandId.HasValue && brandsDict.ContainsKey(p.BrandId.Value) ? brandsDict[p.BrandId.Value] : null,
            Images = imagesDict.ContainsKey(p.ProductId) ? imagesDict[p.ProductId] : new List<ProductImageDto>()
        }).ToList();

        await AttachCompatibilityMetadataAsync(viewModels, compatibilityInfos, allowedSubModelKeys);

        return viewModels;
    }

    /// <summary>
    /// brand_index'ten brand'leri çek
    /// </summary>
    private async Task<List<BrandDto>> GetBrandsByIds(List<int> brandIds)
    {
        if (!brandIds.Any()) return new List<BrandDto>();

        try
        {
            var response = await _elasticSearchService._client.SearchAsync<BrandDto>(s => s
                .Index("brand_index")
                .Size(brandIds.Count)
                .Query(q => q.Terms(t => t.Field("Id").Terms(brandIds)))
            );

            return response.IsValid ? response.Documents.ToList() : new List<BrandDto>();
        }
        catch
        {
            return new List<BrandDto>();
        }
    }

    /// <summary>
    /// image_index'ten image'ları çek
    /// </summary>
    private async Task<List<ProductImageDto>> GetImagesByProductIds(List<int> productIds)
    {
        if (!productIds.Any()) return new List<ProductImageDto>();

        try
        {
            var response = await _elasticSearchService._client.SearchAsync<ImageIndexDto>(s => s
                .Index("image_index")
                .Size(productIds.Count * 3) // Max 3 image per product
                .Query(q => q.Terms(t => t.Field(f => f.ProductId).Terms(productIds)))
            );

            if (!response.IsValid) return new List<ProductImageDto>();

            // ImageIndexDto -> ProductImageDto mapping
            return response.Documents.Select(img => new ProductImageDto
            {
                Id = img.Id,
                ProductId = img.ProductId,
                FileName = img.FileName,
                FileGuid = img.FileGuid,
                CreatedDate = img.CreatedDate,
                ModifiedDate = img.ModifiedDate
            }).ToList();
        }
        catch
        {
            return new List<ProductImageDto>();
        }
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

            var metadata = await _synonymService.GetSearchMetadataAsync();
            var processedKeyword = SearchEngineHelper.ProcessRomanNumerals(keyword, metadata.RomanNumerals, metadata.TechnicalVTerms);
            var searchWords = processedKeyword.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var synonymDict = metadata.Synonyms;
            var expandedTerms = SearchEngineHelper.ExpandSynonyms(searchWords, synonymDict);

            var searchResponse = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Size(20)
                .Sort(st => st
                    .Script(sc => sc
                        .Type("number")
                        .Descending()
                        .Script(scr => scr.Source("doc['Stock'].value > 0 ? 1 : 0"))
                    )
                    .Descending("_score") // Relevance first!
                    .Ascending(p => p.SalePrice)
                )
                .Query(q => q.Bool(b =>
                {
                    var boolDescriptor = b;
                    var mustQueries = new List<QueryContainer>();

                    // Her bir orijinal kelime mutlaka bulunmalı (Synonym'leri ile birlikte)
                    foreach (var originalWord in searchWords)
                    {
                        var termsForThisWord = new List<string> { originalWord };
                        if (synonymDict.TryGetValue(originalWord, out var synonyms))
                        {
                            termsForThisWord.AddRange(synonyms);
                        }
                        termsForThisWord = termsForThisWord.Distinct().ToList();

                        var wordShouldQueries = new List<QueryContainer>();
                        foreach (var term in termsForThisWord)
                        {
                        var smartQuery = SearchEngineHelper.BuildSmartSearchQuery(term, metadata);
                        wordShouldQueries.Add(smartQuery);
                        }

                        mustQueries.Add(new BoolQuery
                        {
                            Should = wordShouldQueries,
                            MinimumShouldMatch = 1
                        });
                    }

                    boolDescriptor.Must(mustQueries.ToArray());

                    // Filters
                    var filters = new List<QueryContainer>();
                    filters.Add(new NumericRangeQuery { Field = "SalePrice", GreaterThan = 0 });
                    filters.Add(new ExistsQuery { Field = "SourceId" });
                    if (onlyInStock)
                    {
                        filters.Add(new NumericRangeQuery { Field = "Stock", GreaterThan = 0 });
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

            var products = searchResponse.Documents.ToList();
            var viewModels = products.Any() ? await JoinRelatedData(products) : new List<SellerProductViewModel>();
            rs.Result = viewModels;
        }
        catch (Exception e)
        {
            rs.AddError($"SearchAsync error: {e.Message}");
        }

        return rs;
    }

    /// <summary>
    /// SellerItemIds ile birden fazla ürün getir
    /// </summary>
    public async Task<IActionResult<List<SellerProductViewModel>>> GetByIdsAsync(List<int> ids)
    {
        var rs = OperationResult.CreateResult<List<SellerProductViewModel>>();
        try
        {
            if (ids == null || !ids.Any())
            {
                rs.Result = new List<SellerProductViewModel>();
                return rs;
            }

            var response = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Size(ids.Count)
                .Query(q => q.Terms(t => t.Field(f => f.SellerItemId).Terms(ids)))
            );

            if (response.IsValid)
            {
                var products = response.Documents.ToList();
                var viewModels = products.Any() ? await JoinRelatedData(products) : new List<SellerProductViewModel>();
                rs.Result = viewModels;
            }
            else
            {
                rs.AddError(response.OriginalException?.Message ?? "Elasticsearch error");
            }
        }
        catch (Exception e)
        {
            rs.AddError(e.Message);
        }

        return rs;
    }
    
    /// <summary>
    /// SellerItemId ile tek ürün getir
    /// </summary>
    public async Task<IActionResult<SellerProductViewModel>> GetByIdAsync(int id)
    {
        var rs = OperationResult.CreateResult<SellerProductViewModel>();
        try
        {
            var response = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Size(1)
                .Query(q => q.Term(t => t.Field(f => f.SellerItemId).Value(id)))
            );

            if (!response.IsValid || !response.Documents.Any())
            {
                rs.AddError($"SellerItem {id} not found");
                return rs;
            }

            var product = response.Documents.First();
            var viewModels = await JoinRelatedData(new List<SellerProductElasticDto> { product });
            var viewModel = viewModels.FirstOrDefault();
            if (viewModel != null)
            {
                rs.Result = viewModel;
            }
            else
            {
                rs.AddError($"SellerItem {id} has no related data");
            }
        }
        catch (Exception e)
        {
            rs.AddError($"GetByIdAsync error: {e.Message}");
        }

        return rs;
    }

    /// <summary>
    /// BrandId ile ürün listesi
    /// </summary>
    public async Task<IActionResult<List<SellerProductViewModel>>> GetByBrandIdAsync(int brandId)
    {
        var rs = OperationResult.CreateResult<List<SellerProductViewModel>>();
        try
        {
            var response = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Size(20)
                .Query(q => q.Bool(b => b
                    .Must(m => m.Term(t => t.Field(f => f.BrandId).Value(brandId)))
                    .Filter(f => f.Range(r => r.Field(fld => fld.SalePrice).GreaterThan(0)),
                            f => f.Exists(e => e.Field(fld => fld.SourceId)))
                ))
                .Sort(ss => ss.Descending(p => p.SellerModifiedDate))
            );

            if (!response.IsValid)
            {
                rs.AddError($"GetByBrandIdAsync failed: {response.OriginalException?.Message}");
                return rs;
            }

            var products = response.Documents.ToList();
            var viewModels = products.Any() ? await JoinRelatedData(products) : new List<SellerProductViewModel>();
            rs.Result = viewModels;
        }
        catch (Exception e)
        {
            rs.AddError($"GetByBrandIdAsync error: {e.Message}");
            return rs;
        }

        return rs;
    }

    public async Task<IActionResult<List<SellerProductViewModel>>> GetByOemCodesAsync(List<string> oemCodes)
    {
        var rs = OperationResult.CreateResult<List<SellerProductViewModel>>();
        try
        {
            if (oemCodes == null || !oemCodes.Any())
            {
                rs.Result = new List<SellerProductViewModel>();
                return rs;
            }

            // Normalizasyon: Boşlukları temizle, büyük harfe çevir
            var normalizedCodes = oemCodes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct()
                .ToList();

            if (!normalizedCodes.Any())
            {
                rs.Result = new List<SellerProductViewModel>();
                return rs;
            }

            var response = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Size(20)
                .Query(q => q.Bool(b => b
                    .Must(m => m.Bool(b2 => 
                    {
                        var shouldQueries = new List<Func<QueryContainerDescriptor<SellerProductElasticDto>, QueryContainer>>();

                        foreach (var code in normalizedCodes)
                        {
                            // 1. PartNumber Match (case-insensitive via analyzer)
                            shouldQueries.Add(sq => sq.Match(m2 => m2.Field(f => f.PartNumber).Query(code).Boost(10.0)));
                            
                            // 2. PartNumber Exact (if mapped as keyword) - try both raw and keyword suffix
                            shouldQueries.Add(sq => sq.Term(t => t.Field(f => f.PartNumber).Value(code).Boost(10.0)));
                            shouldQueries.Add(sq => sq.Term(t => t.Field(f => f.PartNumber.Suffix("keyword")).Value(code).Boost(10.0)));

                            // 3. GroupCode searches
                            shouldQueries.Add(sq => sq.Match(m2 => m2.Field(f => f.OemCode).Query(code).Operator(Operator.And).Boost(5.0)));
                            shouldQueries.Add(sq => sq.Wildcard(w => w.Field(f => f.OemCode).Value($"*{code}*").CaseInsensitive(true).Boost(3.0)));
                            
                            // 4. ProductBarcode fallback
                            shouldQueries.Add(sq => sq.Term(t => t.Field(f => f.ProductBarcode).Value(code).Boost(8.0)));
                        }

                        return b2.Should(shouldQueries.ToArray()).MinimumShouldMatch(1);
                    }))
                    .Filter(f => f.Range(r => r.Field(fld => fld.SalePrice).GreaterThan(0)),
                            f => f.Exists(e => e.Field(fld => fld.SourceId)))
                ))
            );

            if (!response.IsValid)
            {
                rs.AddError($"GetByOemCodesAsync failed: {response.OriginalException?.Message}");
                return rs;
            }

            var products = response.Documents.ToList();
            if (products.Any())
            {
                 // Join related data (images, brands, etc.)
                var viewModels = await JoinRelatedData(products);
                rs.Result = viewModels;
            }
            else
            {
                rs.Result = new List<SellerProductViewModel>();
            }
        }
        catch (Exception e)
        {
            rs.AddError($"GetByOemCodesAsync error: {e.Message}");
        }

        return rs;
    }

    public async Task AttachCompatibilityMetadataAsync(List<SellerProductViewModel>? viewModels)
    {
        await AttachCompatibilityMetadataAsync(viewModels, null, null);
    }

    public async Task AttachCompatibilityMetadataAsync(List<SellerProductViewModel>? viewModels, IEnumerable<string>? allowedSubModelKeys)
    {
        var keySet = BuildAllowedSubModelKeySet(allowedSubModelKeys);
        await AttachCompatibilityMetadataAsync(viewModels, null, keySet);
    }

    private async Task AttachCompatibilityMetadataAsync(
        List<SellerProductViewModel>? viewModels,
        List<CarCompatibilityInfo>? preloadedInfos,
        HashSet<string>? allowedSubModelKeys)
    {
        if(viewModels == null || viewModels.Count == 0){
            return;
        }

        var hasAllowedKeys = allowedSubModelKeys != null && allowedSubModelKeys.Count > 0;

        if (!hasAllowedKeys && preloadedInfos == null)
        {
            // Submodel filtrelenmediyse veya hazır uyumluluk listesi verilmediyse
            // yüzde yüz uyum etiketi göstermiyoruz.
            return;
        }

        try{
            var compatibilityInfos = preloadedInfos ?? await GetUserCompatibilityInfosAsync();
            if(compatibilityInfos == null || compatibilityInfos.Count == 0){
                return;
            }

            if (hasAllowedKeys)
            {
                compatibilityInfos = FilterCompatibilityInfos(compatibilityInfos, allowedSubModelKeys);
                if (compatibilityInfos.Count == 0)
                {
                    return;
                }
            }
            foreach(var product in viewModels){
                var matches = FindCompatibleCars(product, compatibilityInfos, allowedSubModelKeys);
                if(matches.Count == 0){
                    continue;
                }

                product.PerfectCompatibilityCars = matches
                    .Select(info => new SellerProductCompatibilityDto{
                        CarId = info!.Car.Id,
                        PlateNumber = info.Car.PlateNumber,
                        ManufacturerName = info.Car.DotManufacturer?.Name,
                        BaseModelName = info.Car.DotBaseModel?.Name,
                        SubModelName = info.Car.DotSubModel?.Name,
                        ManufacturerKey = info.ManufacturerKeyOriginal,
                        BaseModelKey = info.BaseModelKeyOriginal,
                        SubModelKey = info.SubModelKeyOriginal
                    })
                    .ToList();
            }
        } catch(Exception ex){
            Console.WriteLine($"⚠️ AttachCompatibilityMetadata hatası: {ex.Message}");
        }
    }

    private static List<CarCompatibilityInfo> FindCompatibleCars(
        SellerProductViewModel product,
        List<CarCompatibilityInfo> userCars,
        HashSet<string>? allowedSubModelKeys = null)
    {
        var matches = new List<CarCompatibilityInfo>();

        if(product == null){
            return matches;
        }

        var productManufacturerKey = NormalizeKey(product.ManufacturerKey);
        var productBaseModelKey = NormalizeKey(product.BaseModelKey);

        if(string.IsNullOrEmpty(productManufacturerKey) || string.IsNullOrEmpty(productBaseModelKey)){
            return matches;
        }

        var productSubModelKeys = product.SubModelsJson?
            .Select(sm => NormalizeKey(sm?.Key))
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var restrictToAllowedKeys = allowedSubModelKeys != null && allowedSubModelKeys.Count > 0;

        foreach(var info in userCars){
            if(!string.Equals(info.ManufacturerKey, productManufacturerKey, StringComparison.OrdinalIgnoreCase)){
                continue;
            }

            if(!string.Equals(info.BaseModelKey, productBaseModelKey, StringComparison.OrdinalIgnoreCase)){
                continue;
            }

            if (restrictToAllowedKeys)
            {
                if (string.IsNullOrEmpty(info.SubModelKey) || !allowedSubModelKeys!.Contains(info.SubModelKey))
                {
                    continue;
                }
            }

            if (!string.IsNullOrEmpty(info.SubModelKey))
            {
                if (productSubModelKeys.Contains(info.SubModelKey))
                {
                    matches.Add(info);
                }
                continue;
            }

            if (!restrictToAllowedKeys)
            {
                matches.Add(info);
            }
        }

        return matches;
    }

    private sealed class CarCompatibilityInfo
    {
        private CarCompatibilityInfo(
            UserCars car,
            string manufacturerKey,
            string baseModelKey,
            string? subModelKey,
            string? manufacturerKeyOriginal,
            string? baseModelKeyOriginal,
            string? subModelKeyOriginal)
        {
            Car = car;
            ManufacturerKey = manufacturerKey;
            BaseModelKey = baseModelKey;
            SubModelKey = subModelKey;
            ManufacturerKeyOriginal = manufacturerKeyOriginal;
            BaseModelKeyOriginal = baseModelKeyOriginal;
            SubModelKeyOriginal = subModelKeyOriginal;
        }

        public UserCars Car { get; }
        public string ManufacturerKey { get; }
        public string BaseModelKey { get; }
        public string? SubModelKey { get; }
        public string? ManufacturerKeyOriginal { get; }
        public string? BaseModelKeyOriginal { get; }
        public string? SubModelKeyOriginal { get; }

        public static CarCompatibilityInfo? Create(UserCars car)
        {
            if(car == null){
                return null;
            }

            var manufacturerRaw = car.DotManufacturer?.DatKey ?? car.DotManufacturerKey;
            var baseModelRaw = car.DotBaseModel?.DatKey ?? car.DotBaseModelKey;
            var subModelRaw = car.DotSubModel?.DatKey ?? car.DotSubModelKey;

            var manufacturerKey = NormalizeKey(manufacturerRaw);
            var baseModelKey = NormalizeKey(baseModelRaw);
            var subModelKey = NormalizeKey(subModelRaw);

            if(string.IsNullOrEmpty(manufacturerKey) || string.IsNullOrEmpty(baseModelKey)){
                return null;
            }

            return new CarCompatibilityInfo(
                car,
                manufacturerKey,
                baseModelKey,
                string.IsNullOrEmpty(subModelKey) ? null : subModelKey,
                manufacturerRaw,
                baseModelRaw,
                subModelRaw);
        }
    }

    private static string NormalizeKey(string? key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : key.Trim().ToUpperInvariant();
    }

    private static HashSet<string>? BuildAllowedSubModelKeySet(IEnumerable<string>? keys)
    {
        if (keys == null)
        {
            return null;
        }

        var set = keys
            .Select(NormalizeKey)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return set.Count > 0 ? set : null;
    }

    private static List<CarCompatibilityInfo> FilterCompatibilityInfos(IEnumerable<CarCompatibilityInfo> infos, HashSet<string>? allowedSubModelKeys)
    {
        if (allowedSubModelKeys == null || allowedSubModelKeys.Count == 0)
        {
            return infos.ToList();
        }

        return infos
            .Where(info => !string.IsNullOrEmpty(info.SubModelKey) && allowedSubModelKeys.Contains(info.SubModelKey))
            .ToList();
    }

    private async Task<List<CarCompatibilityInfo>> GetUserCompatibilityInfosAsync()
    {
        try
        {
            var userCarsResult = await _userCarService.GetAllUserCarsForCurrentUserAsync();
            if(!userCarsResult.Ok || userCarsResult.Result == null || userCarsResult.Result.Count == 0){
                return new List<CarCompatibilityInfo>();
            }

            return userCarsResult.Result
                .Select(CarCompatibilityInfo.Create)
                .Where(info => info != null)
                .Cast<CarCompatibilityInfo>()
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ GetUserCompatibilityInfosAsync hatası: {ex.Message}");
            return new List<CarCompatibilityInfo>();
        }
    }

    private async Task<Paging<List<SellerProductViewModel>>> GetPerfectCompatibilityPage(
        SearchFilterReguestDto filter,
        BoolQuery baseQuery,
        Func<SortDescriptor<SellerProductElasticDto>, IPromise<IList<ISort>>>? sortFunc,
        List<CarCompatibilityInfo> compatibilityInfos,
        HashSet<string>? allowedSubModelKeys)
    {
        var skip = Math.Max((filter.Page - 1) * filter.PageSize, 0);

        if (allowedSubModelKeys != null && allowedSubModelKeys.Count > 0)
        {
            compatibilityInfos = FilterCompatibilityInfos(compatibilityInfos, allowedSubModelKeys);
        }

        var comboInfos = compatibilityInfos
            .Where(ci => !string.IsNullOrWhiteSpace(ci.ManufacturerKey) && !string.IsNullOrWhiteSpace(ci.BaseModelKey))
            .GroupBy(ci => (ci.ManufacturerKey, ci.BaseModelKey))
            .Select(g => g.First())
            .ToList();

        if (comboInfos.Count == 0)
        {
            return new Paging<List<SellerProductViewModel>>
            {
                Data = new List<SellerProductViewModel>(),
                DataCount = 0
            };
        }

        var collectedProducts = new Dictionary<int, SellerProductElasticDto>();
        var sortDescriptor = sortFunc ?? ResolveSort(filter.Sort);

        foreach (var info in comboInfos)
        {
            var manufacturerShould = new List<QueryContainer>
            {
                new TermQuery { Field = "ManufacturerKey", Value = info.ManufacturerKey, CaseInsensitive = true }
            };

            if (!string.IsNullOrWhiteSpace(info.ManufacturerKeyOriginal))
            {
                manufacturerShould.Add(new TermQuery
                {
                    Field = "ManufacturerKey",
                    Value = info.ManufacturerKeyOriginal?.Trim(),
                    CaseInsensitive = true
                });
            }

            var manufacturerQuery = new BoolQuery
            {
                Should = manufacturerShould,
                MinimumShouldMatch = 1
            };

            var baseModelShould = new List<QueryContainer>
            {
                new TermQuery { Field = "BaseModelKey", Value = info.BaseModelKey, CaseInsensitive = true }
            };

            if (!string.IsNullOrWhiteSpace(info.BaseModelKeyOriginal))
            {
                baseModelShould.Add(new TermQuery
                {
                    Field = "BaseModelKey",
                    Value = info.BaseModelKeyOriginal?.Trim(),
                    CaseInsensitive = true
                });
            }

            var baseModelQuery = new BoolQuery
            {
                Should = baseModelShould,
                MinimumShouldMatch = 1
            };

            var comboQuery = new BoolQuery
            {
                Must = new List<QueryContainer>
                {
                    manufacturerQuery,
                    baseModelQuery
                }
            };

            var combinedMust = baseQuery.Must?.ToList() ?? new List<QueryContainer>();
            combinedMust.Add(comboQuery);

            var query = new BoolQuery
            {
                Must = combinedMust,
                Filter = baseQuery.Filter,
                Should = baseQuery.Should,
                MinimumShouldMatch = baseQuery.MinimumShouldMatch,
                MustNot = baseQuery.MustNot
            };

            var response = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Query(_ => query)
                .Sort(sortDescriptor)
                .Size(Math.Clamp(filter.PageSize * 6, 120, 600))
            );

            if (!response.IsValid || response.Documents == null)
            {
                Console.WriteLine($"⚠️ Perfect compatibility combo sorgusu başarısız: {response.OriginalException?.Message}");
                continue;
            }

            foreach (var doc in response.Documents)
            {
                if (!collectedProducts.ContainsKey(doc.SellerItemId))
                {
                    collectedProducts[doc.SellerItemId] = doc;
                }
            }
        }

        if (collectedProducts.Count == 0)
        {
            return new Paging<List<SellerProductViewModel>>
            {
                Data = new List<SellerProductViewModel>(),
                DataCount = 0
            };
        }

        var viewModels = await JoinRelatedData(collectedProducts.Values.ToList(), compatibilityInfos, allowedSubModelKeys);
        var compatibleProducts = viewModels.Where(vm => vm.PerfectCompatibilityCars?.Any() == true).ToList();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var filteredBySearch = FilterBySearch(compatibleProducts, filter.Search);
            if (filteredBySearch.Count > 0)
            {
                compatibleProducts = filteredBySearch;
            }
        }

        if (compatibleProducts.Count == 0)
        {
            return new Paging<List<SellerProductViewModel>>
            {
                Data = new List<SellerProductViewModel>(),
                DataCount = 0
            };
        }

        var sortedProducts = ApplySorting(compatibleProducts, filter.Sort);
        var pagedProducts = sortedProducts.Skip(skip).Take(filter.PageSize).ToList();

        return new Paging<List<SellerProductViewModel>>
        {
            Data = pagedProducts,
            DataCount = sortedProducts.Count
        };
    }

    private static List<SellerProductViewModel> FilterBySearch(List<SellerProductViewModel> products, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return products;
        }

        var words = search
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (words.Length == 0)
        {
            return products;
        }

        return products
            .Where(product => MatchesSearch(product, words))
            .ToList();
    }

    private static bool MatchesSearch(SellerProductViewModel product, string[] words)
    {
        if (product == null)
        {
            return false;
        }

        var candidateFields = new List<string?>
        {
            product.ProductName,
            product.ManufacturerName,
            product.DotPartName,
            product.ProductDescription,
            product.PartNumber,
            product.BaseModelName,
            product.DotPartDescription
        };
        
        // GroupCode artık array - tüm elemanlarını ekle
        if (product.OemCode != null)
        {
            candidateFields.AddRange(product.OemCode);
        }

        if (product.Brand?.Name != null)
        {
            candidateFields.Add(product.Brand.Name);
        }

        bool ContainsWord(string? source, string word) =>
            !string.IsNullOrWhiteSpace(source) && source.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;

        if (words.Length == 1)
        {
            var single = words[0];
            return candidateFields.Any(field => ContainsWord(field, single));
        }

        return words.All(word => candidateFields.Any(field => ContainsWord(field, word)));
    }

    private static List<SellerProductViewModel> ApplySorting(List<SellerProductViewModel> products, ProductFilter sort)
    {
        // Önce stokta olanları (Stock > 0) başa getir
        var baseQuery = products.OrderByDescending(p => p.Stock > 0);

        return sort switch
        {
            ProductFilter.ByPriceAsc => baseQuery.ThenBy(p => p.SalePrice).ToList(),
            ProductFilter.ByPriceDesc => baseQuery.ThenByDescending(p => p.SalePrice).ToList(),
            ProductFilter.ByStockAsc => baseQuery.ThenBy(p => p.Stock).ToList(),
            ProductFilter.ByStockDesc => baseQuery.ThenByDescending(p => p.Stock).ToList(),
            ProductFilter.productNameAsc => baseQuery.ThenBy(p => p.ProductName).ToList(),
            ProductFilter.productNamedesc => baseQuery.ThenByDescending(p => p.ProductName).ToList(),
            ProductFilter.ByCreationDateAsc => baseQuery.ThenBy(p => p.SellerModifiedDate ?? DateTime.MinValue).ToList(),
            _ => baseQuery.ToList()
        };
    }

    private static Func<SortDescriptor<SellerProductElasticDto>, IPromise<IList<ISort>>>? ResolveSort(ProductFilter sort)
    {
        return s =>
        {
            // Her zaman stokta olanları (Stock > 0) en başa getir
            s.Script(sc => sc
                .Type("number")
                .Descending()
                .Script(scr => scr.Source("doc['Stock'].value > 0 ? 1 : 0"))
            );

            // Ardından seçili sıralama kriterini uygula
            return sort switch
            {
                ProductFilter.ByPriceAsc => s.Ascending(p => p.SalePrice).Descending("_score"),
                ProductFilter.ByPriceDesc => s.Descending(p => p.SalePrice).Descending("_score"),
                ProductFilter.ByStockAsc => s.Ascending(p => p.Stock).Descending("_score"),
                ProductFilter.ByStockDesc => s.Descending(p => p.Stock).Descending("_score"),
                ProductFilter.ByCreationDateAsc => s.Ascending(p => p.SellerModifiedDate).Descending("_score"),
                ProductFilter.productNameAsc => s.Field(f => f.Field(p => p.ProductName.Suffix("keyword")).Order(SortOrder.Ascending)).Descending("_score"),
                ProductFilter.productNamedesc => s.Field(f => f.Field(p => p.ProductName.Suffix("keyword")).Order(SortOrder.Descending)).Descending("_score"),
                _ => s.Descending("_score").Ascending(p => p.SalePrice) // Default: Score then Price
            };
        };
    }

    /// <summary>
    /// ProductId'ye göre uyumlu Models/SubModels hiyerarşisini döndürür
    /// </summary>
    public async Task<IActionResult<List<ecommerce.Web.Domain.Dtos.BaseModelDto>>> GetCompatibleModelsAsync(int productId)
    {
        var rs = OperationResult.CreateResult<List<ecommerce.Web.Domain.Dtos.BaseModelDto>>();
        try
        {
            // 1. Önce product bilgilerini alalım
            var productResponse = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index("sellerproduct_index")
                .Size(1)
                .Query(q => q.Term(t => t.Field(f => f.ProductId).Value(productId)))
            );

            if (!productResponse.IsValid || !productResponse.Documents.Any())
            {
                rs.AddError($"Product {productId} not found");
                return rs;
            }

            var product = productResponse.Documents.First();
            
            // 2. Product'tan Base Model bilgisi oluştur
            var compatibleModels = new List<ecommerce.Web.Domain.Dtos.BaseModelDto>();
            
            if (!string.IsNullOrEmpty(product.BaseModelName) && !string.IsNullOrEmpty(product.BaseModelKey))
            {
                var baseModel = new ecommerce.Web.Domain.Dtos.BaseModelDto
                {
                    Id = 0, // ID yok, gerekirse ekleriz
                    Name = product.BaseModelName,
                    VehicleType = product.VehicleType ?? 0,
                    ManufacturerKey = product.ManufacturerKey ?? "",
                    BaseModelKey = product.BaseModelKey,
                    ManufacturerName = product.ManufacturerName,
                    ImageUrl = product.DocumentUrl, // Product image'ı kullan
                    SubModels = product.SubModelsJson?
                        .Where(sm => !string.IsNullOrWhiteSpace(sm.Name)) // İsmi boş olanları filtrele
                        .Select(sm => new ecommerce.Web.Domain.Dtos.SubModelDto
                        {
                            Name = sm.Name ?? "",
                            SubModelKey = sm.Key ?? "",
                            ImageUrl = null
                        })
                        .ToList() ?? new List<ecommerce.Web.Domain.Dtos.SubModelDto>()
                };
                
                compatibleModels.Add(baseModel);
            }

            rs.Result = compatibleModels;
        }
        catch (Exception e)
        {
            rs.AddError($"GetCompatibleModelsAsync error: {e.Message}");
        }

        return rs;
    }
}

