using System;
using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Events;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.JSInterop;
using Radzen;
using System.Linq;
using System.Globalization;
using System.Threading;
namespace ecommerce.Web.Components.Pages;
public partial class CategoryLeftSideBar : IAsyncDisposable {
    private ElementReference scrollSentinel;
    private DotNetObjectReference<CategoryLeftSideBar>? dotNetRef;
    private bool _isDisposed;
    [Parameter] [SupplyParameterFromQuery] public int ? catid{get;set;}
    [Parameter] [SupplyParameterFromQuery] public string ? catname{get;set;}
    [Parameter] [SupplyParameterFromQuery] public int ? brandid{get;set;}
    [Parameter] [SupplyParameterFromQuery] public string ? brandname{get;set;}
    [Parameter] [SupplyParameterFromQuery] public string ? query{get;set;}
    [Parameter] [SupplyParameterFromQuery] public string? manufacturerkey { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? manufacturername { get; set; }
    [Parameter] [SupplyParameterFromQuery] public int? modelid { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? modelkey { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? modelname { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? partnumber { get; set; }
    [Parameter] [SupplyParameterFromQuery(Name = "onlyperfectcompatibility")] public bool? onlyPerfectCompatibilityQuery { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? submodelkey { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? submodelname { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? dotpartname { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? datprocessnumber { get; set; }
    [Parameter] [SupplyParameterFromQuery(Name = "onlyinstock")] public bool? onlyInStockQuery { get; set; }
    [Inject] private AppStateManager _appStateManager{get;set;}
    [Inject] private NotificationService _notificationService{get;set;}
    [Inject] private IFavoriteService FavoriteService{get;set;}
    [Inject] private II18N lang{get;set;}
    [Inject] private CdnOptions CdnConfig{get;set;}
    [Inject] private IJSRuntime _jsRuntime{get;set;}
    [Inject] private NavigationManager NavigationManager{get;set;}
    [Inject] private ISellerProductService ProductService{get;set;}
    [Inject] private ICategoryService CategoryService{get;set;}
    private int page = 1;
    private int currentUserId;
    private int pageSize = 20;
    private int totalCount = 0;
    private int CurrentPage => page;
    private int TotalPages => (int) Math.Ceiling((double) totalCount / pageSize);
    private List<CategoryDto> category = new();
    private List<BrandDto> brands = new();
    private List<string> dotPartNames = new();
    private List<string> manufacturerNames = new();
    private List<string> baseModelNames = new();
    private List<string> subModelNames = new();
    private List<SellerProductViewModel> Products = new();
    private ecommerce.Web.Domain.Dtos.Cart.CartDto CartResult = new();
    private CancellationTokenSource? _renderCts;
    private int selectedSort{get;set;} = 0;
    private List<int> SelectedCategoryIds{get;set;} = new();
    private List<int> SelectedBrandIds{get;set;} = new();
    private HashSet<string> SelectedDotPartNames { get; set; } = new();
    private HashSet<string> SelectedManufacturerNames { get; set; } = new();
    private HashSet<string> SelectedBaseModelNames { get; set; } = new();
    private HashSet<string> SelectedSubModelNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    private bool IsAnyFilterSelected => SelectedCategoryIds.Any() || SelectedBrandIds.Any() || SelectedDotPartNames.Any() || SelectedManufacturerNames.Any() || SelectedBaseModelNames.Any() || SelectedSubModelNames.Any();
    private bool IsMobileFilterOpen { get; set; }
    private const string ChipTypeCategory = "category";
    private const string ChipTypeBrand = "brand";
    private const string ChipTypeDotPart = "dotPart";
    private const string ChipTypeManufacturer = "manufacturer";
    private const string ChipTypeBaseModel = "baseModel";
    private const string ChipTypeSubModel = "subModel";
    private IEnumerable<FilterChip> ActiveFilterChips
    {
        get
        {
            var chips = new List<FilterChip>();

            foreach (var id in SelectedCategoryIds)
            {
                var name = category.FirstOrDefault(x => x.Id == id)?.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    chips.Add(new FilterChip(ChipTypeCategory, name, intValue: id));
                }
            }

            foreach (var id in SelectedBrandIds)
            {
                var name = brands.FirstOrDefault(x => x.Id == id)?.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    chips.Add(new FilterChip(ChipTypeBrand, name, intValue: id));
                }
            }

            foreach (var name in SelectedDotPartNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    chips.Add(new FilterChip(ChipTypeDotPart, name, stringValue: name));
                }
            }

            foreach (var name in SelectedManufacturerNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    chips.Add(new FilterChip(ChipTypeManufacturer, name, stringValue: name));
                }
            }

            foreach (var name in SelectedBaseModelNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    chips.Add(new FilterChip(ChipTypeBaseModel, name, stringValue: name));
                }
            }

            foreach (var name in SelectedSubModelNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    chips.Add(new FilterChip(ChipTypeSubModel, name, stringValue: name));
                }
            }

            return chips;
        }
    }
    private ProductFilter _productFilter;
    private string searchCategory = "";
    private string searchBrand = "";
    private string productSearch = string.Empty;
    private string searchDotPart = string.Empty;
    private string searchManufacturer = string.Empty;
    private string searchBaseModel = string.Empty;
    private string searchSubModel = string.Empty;
    private bool onlyPerfectCompatibilityFilter;
    private bool onlyInStockFilter;
    private bool HasVehicleQuery => !string.IsNullOrWhiteSpace(manufacturerkey) || modelid.HasValue || !string.IsNullOrWhiteSpace(modelkey) || !string.IsNullOrWhiteSpace(submodelkey);
    private bool ShouldApplyPerfectCompatibility => onlyPerfectCompatibilityFilter && !HasVehicleQuery;

    // INFINITE SCROLL
    private bool _isLoadingMore = false;
    private bool HasMoreProducts => CurrentPage < TotalPages;

    protected override async Task OnInitializedAsync()
    {
        _appStateManager.StateChanged += AppState_StateChanged;
        CartResult = await _appStateManager.GetCart();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_isDisposed) return;
        
        productSearch = query ?? string.Empty;
        onlyPerfectCompatibilityFilter = onlyPerfectCompatibilityQuery ?? false;
        onlyInStockFilter = onlyInStockQuery ?? false;
        if (!string.IsNullOrWhiteSpace(submodelname))
        {
            var normalizedSubModelName = NormalizeFriendlyName(submodelname);
            if (!string.IsNullOrWhiteSpace(normalizedSubModelName) && !SelectedSubModelNames.Contains(normalizedSubModelName))
            {
                SelectedSubModelNames.Add(normalizedSubModelName);
            }
        }
        
        if (!string.IsNullOrWhiteSpace(dotpartname))
        {
             var decodedPart = Uri.UnescapeDataString(dotpartname);
             if(!SelectedDotPartNames.Contains(decodedPart))
             {
                 SelectedDotPartNames.Add(decodedPart);
             }
        }

        try{
            await LoadProducts();
        } catch(Exception ex){
            Console.WriteLine($"⚠️ OnParametersSetAsync hatası: {ex.Message}");
        }
    }

    private async void AppState_StateChanged(ComponentBase source, string property, ecommerce.Web.Domain.Dtos.Cart.CartDto? updatedCart)
    {
        if (_isDisposed || property != AppStateEvents.updateCart)
        {
            return;
        }

        try
        {
            await InvokeAsync(async () => {
                CartResult = updatedCart ?? await _appStateManager.GetCart();
                await RequestRender();
            });
        }
        catch (Exception ex)
        {
            if (!_isDisposed)
                Console.WriteLine($"⚠️ AppState_StateChanged error: {ex.Message}");
        }
    }

    private async Task RequestRender()
    {
        if (_isDisposed) return;

        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _renderCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        try
        {
            var token = newCts.Token;
            await Task.Delay(30, token); // Increased debounce window for large lists
            
            if (!token.IsCancellationRequested && !_isDisposed)
            {
                await InvokeAsync(async () => {
                    if (!token.IsCancellationRequested && !_isDisposed)
                    {
                         base.StateHasChanged();
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!_isDisposed)
                Console.WriteLine($"⚠️ RequestRender error: {ex.Message}");
        }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender){
        if(firstRender){
            try {
                await Task.Delay(500);
   
                await _jsRuntime.InvokeVoidAsync("sliderThree");

                dotNetRef = DotNetObjectReference.Create(this);
                await _jsRuntime.InvokeVoidAsync("setupInfiniteScroll", scrollSentinel, dotNetRef);
            }
            catch (Exception ex) {
                if (!_isDisposed)
                    Console.WriteLine($"⚠️ OnAfterRenderAsync failed: {ex.Message}");
            }
        }
    }
    
    private async Task ScrollToLoadMore(){
       
        if(HasMoreProducts && !_isLoadingMore){
            await LoadMoreProducts();
        }
    }
    
    [JSInvokable]
    public async Task LoadMoreProducts(){
        if(!HasMoreProducts || _isLoadingMore || _isDisposed) return;
        
        _isLoadingMore = true;
        // Do not call StateHasChanged here if we are about to call ExecuteWithLoading
        
        try {
            await _appStateManager.ExecuteWithLoading(async () => {
                page++;
                await LoadNextPage();
            }, lang["Common.LoadingMore"] ?? "Diğer ürünler yükleniyor");
        } catch (Exception ex) {
            Console.WriteLine($"⚠️ Infinite scroll error: {ex.Message}");
        } finally {
            if (!_isDisposed)
            {
                _isLoadingMore = false;
                await RequestRender();
            }
        }
    }
    private List<string> BuildSubModelNameFilters()
    {
        var names = SelectedSubModelNames.ToList();

        if (!string.IsNullOrWhiteSpace(submodelname) && !names.Any(n => string.Equals(n, submodelname, StringComparison.OrdinalIgnoreCase)))
        {
            names.Add(submodelname);
        }

        return names;
    }

    private List<string> BuildSubModelKeyFilters()
    {
        if (!string.IsNullOrWhiteSpace(submodelkey))
        {
            return new List<string> { submodelkey };
        }

        return new List<string>();
    }

    private async Task LoadProducts(bool resetPaging = true){
        await _appStateManager.ExecuteWithLoading(async () => {
            try{
                if(resetPaging){
                    page = 1; 
                    Products = new List<SellerProductViewModel>(); 
                }
                
                var subModelNamesFilter = BuildSubModelNameFilters();
                var subModelKeysFilter = BuildSubModelKeyFilters();
                
                // partnumber parametresi varsa, hem PartNumber field'ında hem de Search'te ara
                var searchQuery = string.IsNullOrWhiteSpace(productSearch) ? query : productSearch;
                if (!string.IsNullOrWhiteSpace(partnumber) && string.IsNullOrWhiteSpace(searchQuery))
                {
                    searchQuery = partnumber;
                }
                
                // CRITICAL FIX: URL parametrelerini filter listelerine ekle
                // Aksi takdirde manufacturername=bmw var ama ManufacturerNames listesi boş kalıyor!
                var manufacturerNamesFilter = SelectedManufacturerNames.ToList();
                Console.WriteLine($"🔍 Initial SelectedManufacturerNames: {string.Join(", ", manufacturerNamesFilter)}");
                Console.WriteLine($"🔍 URL manufacturername: {manufacturername ?? "null"}");
                
                if (!string.IsNullOrWhiteSpace(manufacturername) && !manufacturerNamesFilter.Contains(manufacturername, StringComparer.OrdinalIgnoreCase))
                {
                    manufacturerNamesFilter.Add(manufacturername);
                    Console.WriteLine($"✅ Added manufacturername from URL");
                }
                else if (!string.IsNullOrWhiteSpace(manufacturername))
                {
                    Console.WriteLine($"ℹ️ manufacturername already in list (case-insensitive)");
                }
                
                // Ensure no duplicates
                var beforeDistinct = manufacturerNamesFilter.Count;
                manufacturerNamesFilter = manufacturerNamesFilter.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (beforeDistinct != manufacturerNamesFilter.Count)
                {
                    Console.WriteLine($"⚠️ Removed {beforeDistinct - manufacturerNamesFilter.Count} duplicate manufacturers");
                }
                
                var baseModelNamesFilter = SelectedBaseModelNames.ToList();
                if (!string.IsNullOrWhiteSpace(modelname) && !baseModelNamesFilter.Contains(modelname, StringComparer.OrdinalIgnoreCase))
                {
                    baseModelNamesFilter.Add(modelname);
                }
                // Ensure no duplicates
                baseModelNamesFilter = baseModelNamesFilter.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                
                Console.WriteLine($"🔎 CategoryLeftSideBar Search Params:");
                Console.WriteLine($"   ManufacturerNames: {string.Join(", ", manufacturerNamesFilter)}");
                Console.WriteLine($"   BaseModelNames: {string.Join(", ", baseModelNamesFilter)}");
                Console.WriteLine($"   SubModelKeys: {string.Join(", ", subModelKeysFilter)}");
                Console.WriteLine($"   DatProcessNumber: {datprocessnumber}");
                Console.WriteLine($"   OnlyInStock: {onlyInStockFilter}");
                
               var response = await ProductService.GetByFilterPagingAsync(new SearchFilterReguestDto{
                        CategoryId = catid.HasValue ? catid.Value : null,
                        Page = page,
                        PageSize = pageSize,
                        Search = searchQuery,
                        BrandId = brandid.HasValue ? brandid.Value : null,
                        ManufacturerKey = manufacturerkey,
                        BaseModelKey = modelkey,
                        ModelId = modelid,
                        PartNumber = partnumber,
                        DotPartNames = SelectedDotPartNames.ToList(),
                        DatProcessNumbers = !string.IsNullOrWhiteSpace(datprocessnumber) ? new List<string> { datprocessnumber } : null,
                        ManufacturerNames = manufacturerNamesFilter,
                        BaseModelNames = baseModelNamesFilter,
                        SubModelNames = subModelNamesFilter,
                        SubModelKeys = subModelKeysFilter,
                        OnlyPerfectCompatibility = ShouldApplyPerfectCompatibility,
                        OnlyInStock = onlyInStockFilter,
                        // Pass names for Backend Fallback Logic
                        SingleManufacturerName = manufacturername,
                        SingleModelName = modelname,
                        SingleSubModelName = submodelname
                    }
                );
                
                Console.WriteLine($"✅ Search Response: {response.Result?.Data?.Count ?? 0} products");

                if (_isDisposed) return;

                var hasVehicleFilter = !string.IsNullOrWhiteSpace(manufacturerkey) || !string.IsNullOrWhiteSpace(modelkey) || modelid.HasValue || !string.IsNullOrWhiteSpace(submodelkey);
                
                // Backend'de fallback var, burada sadece submodelkey varsa compatibility metadata ekle
                if(catid.HasValue){
                    category.Clear();
                    var data = await CategoryService.GetCatehoryWithById(catid.Value);
                    if (_isDisposed) return;
                    if(data.Result != null){
                        foreach(var item in data.Result){
                            if(item == null) continue;
                            var existing = category.FirstOrDefault(x => x.Id == item.Id);
                            if(existing != null){
                                if(existing.Name != item.Name || existing.ParentId != item.ParentId){
                                    category.Remove(existing);
                                    category.Add(new CategoryDto{Id = item.Id, ParentId = item.ParentId, Name = item.Name});
                                }
                            } else{
                                category.Add(new CategoryDto{Id = item.Id, ParentId = item.ParentId, Name = item.Name});
                            }
                        }
                    }
                } else{
                    category.Clear();
                    if(response.Result?.Data != null){
                        var categories = response.Result.Data;
                        if(categories is{Count: > 0}){
                            foreach(var aa in categories){
                                if(aa.Categories == null) continue;
                                foreach(var item in aa.Categories){
                                    if(item == null) continue;
                                    var existing = category.FirstOrDefault(x => x.Id == item.Id);
                                    if(existing != null){
                                        if(existing.Name == item.Name && existing.ParentId == item.ParentId) continue;
                                        category.Remove(existing);
                                        category.Add(new CategoryDto{Id = item.Id, ParentId = item.ParentId, Name = item.Name});
                                    } else{
                                        category.Add(new CategoryDto{Id = item.Id, ParentId = item.ParentId, Name = item.Name});
                                    }
                                }

                            }
                        }
                    }
                }
                if (_isDisposed) return;
                if(response.Ok && response.Result != null){
                    Products = response.Result.Data ?? new List<SellerProductViewModel>();
                    if (subModelKeysFilter?.Any() == true)
                    {
                        await ProductService.AttachCompatibilityMetadataAsync(Products, subModelKeysFilter);
                    }
                    if (_isDisposed) return;
                    totalCount = response.Result.DataCount;
                    
                    brands.Clear();
                    dotPartNames.Clear();
                    manufacturerNames.Clear();
                    baseModelNames.Clear();
                    subModelNames.Clear();
                    if(response.Result.Data != null){
                        foreach(var brand in response.Result.Data){
                            if(brand.Brand == null) continue;
                            var exitsBrand = brands.FirstOrDefault(x => x.Id == brand.Brand.Id);
                            if(exitsBrand != null){
                                if(exitsBrand.Name != brand.Brand.Name){
                                    brands.Remove(exitsBrand);
                                    brands.Add(new BrandDto{Id = brand.Brand.Id, Name = brand.Brand.Name});
                                }
                            } else{
                                brands.Add(new BrandDto{Id = brand.Brand.Id, Name = brand.Brand.Name});
                            }

                            if(!string.IsNullOrWhiteSpace(brand.DotPartName))
                            {
                                dotPartNames.Add(brand.DotPartName);
                            }

                            if(!string.IsNullOrWhiteSpace(brand.ManufacturerName))
                            {
                                manufacturerNames.Add(brand.ManufacturerName);
                            }

                            if(!string.IsNullOrWhiteSpace(brand.BaseModelName))
                            {
                                baseModelNames.Add(brand.BaseModelName);
                            }

                            if (brand.SubModelsJson?.Any() == true)
                            {
                                foreach (var sm in brand.SubModelsJson)
                                {
                                    if (!string.IsNullOrWhiteSpace(sm?.Name))
                                    {
                                        subModelNames.Add(sm!.Name!);
                                    }
                                }
                            }
                        }
                        dotPartNames = dotPartNames.Distinct().OrderBy(x => x).ToList();
                        manufacturerNames = manufacturerNames.Distinct().OrderBy(x => x).ToList();
                        baseModelNames = baseModelNames.Distinct().OrderBy(x => x).ToList();
                        subModelNames = subModelNames.Distinct().OrderBy(x => x).ToList();
                    }
                } else {
                    var errorMsg = response.Metadata?.Message ?? response.Exception?.Message ?? "Ürünler yüklenemedi";
                    Console.WriteLine($"⚠️ LoadProducts hatası: {errorMsg}");
                    Products = new List<SellerProductViewModel>();
                    totalCount = 0;
                    dotPartNames.Clear();
                    manufacturerNames.Clear();
                    baseModelNames.Clear();
                    subModelNames.Clear();
                }
                if (_isDisposed) return;
                await RequestRender();
            } catch(Exception e){
                Console.WriteLine($"⚠️ LoadProducts hatası: {e.Message}");
                Products = new List<SellerProductViewModel>();
                totalCount = 0;
                dotPartNames.Clear();
                manufacturerNames.Clear();
                baseModelNames.Clear();
                subModelNames.Clear();
            }
        }, lang["Common.Loading"] ?? "Yükleniyor...");
    }
    
    private async Task LoadNextPage(){
        try{
            var subModelNamesFilter = BuildSubModelNameFilters();
            var subModelKeysFilter = BuildSubModelKeyFilters();
            
            // partnumber parametresi varsa ve productSearch boşsa, partnumber'ı Search'e ekle
            var searchQuery = productSearch;
            if (!string.IsNullOrWhiteSpace(partnumber) && string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = partnumber;
            }
            
            var response = await ProductService.GetByFilterPagingAsync(new SearchFilterReguestDto{
                    CategoryIds = SelectedCategoryIds,
                    BrandIds = SelectedBrandIds,
                    CategoryId = catid,
                    BrandId = brandid,
                    Page = page,
                    PageSize = pageSize,
                    Search = searchQuery,
                    Sort = _productFilter,
                    ManufacturerKey = manufacturerkey,
                    BaseModelKey = modelkey,
                    ModelId = modelid,
                    PartNumber = partnumber,
                    DotPartNames = SelectedDotPartNames.ToList(),
                    ManufacturerNames = SelectedManufacturerNames.ToList(),
                    BaseModelNames = SelectedBaseModelNames.ToList(),
                    SubModelNames = subModelNamesFilter,
                    SubModelKeys = subModelKeysFilter,
                    OnlyPerfectCompatibility = ShouldApplyPerfectCompatibility,
                    OnlyInStock = onlyInStockFilter,
                    SingleManufacturerName = manufacturername,
                    SingleModelName = modelname,
                    SingleSubModelName = submodelname
                }
            );
            
            if(response.Ok && response.Result != null && response.Result.Data != null){
                if (subModelKeysFilter?.Any() == true)
                {
                    await ProductService.AttachCompatibilityMetadataAsync(response.Result.Data, subModelKeysFilter);
                }

                await InvokeAsync(async () => {
                    Products.AddRange(response.Result.Data);
                    totalCount = response.Result.DataCount;
                    await RequestRender();
                });
            }
        } catch(Exception e){
            Console.WriteLine($"⚠️ LoadNextPage error: {e.Message}");
        }
    }
    private async Task OnPageChanged(int newPage){
        page = newPage;
        try{
            await LoadProducts();
            await _jsRuntime.InvokeVoidAsync("window.scrollTo", 0, 0);
        } catch(Exception e){
            Console.WriteLine($"⚠️ OnPageChanged hatası: {e.Message}");
        }
    }
    private string GetEnumDescription(ProductFilter value){
        var field = value.GetType().GetField(value.ToString());
        if (field == null)
        {
            return value.ToString();
        }

        var attribute = Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) as DisplayAttribute;
        return attribute?.Description ?? value.ToString();
    }
    private async Task OnSortChanged(ChangeEventArgs e){
        if(int.TryParse(e.Value?.ToString(), out var selected)){
            selectedSort = selected;
            _productFilter = (ProductFilter) selected;
        }
        await GetProductService();
    }
    private async Task OnSearchSubmit()
    {
        page = 1; 
        await GetProductService();
    }
    private async Task OnCategoryChanged(ChangeEventArgs e, int id){
        if((bool) e.Value!){
            if(!SelectedCategoryIds.Contains(id)) SelectedCategoryIds.Add(id);
        } else{
            SelectedCategoryIds.Remove(id);
        }
        await RequestRender();
        await GetProductService();
    }
    private async Task OnBrandChanged(ChangeEventArgs e, int id){
        if((bool) e.Value!){
            if(!SelectedBrandIds.Contains(id)) SelectedBrandIds.Add(id);
        } else{
            SelectedBrandIds.Remove(id);
        }
        await RequestRender();
        await GetProductService();
    }
    private async Task GetProductService(){
        await _appStateManager.ExecuteWithLoading(async () => {
            try{
                // Reset paging when filters change
                page = 1;
                var subModelNamesFilter = BuildSubModelNameFilters();
                var subModelKeysFilter = BuildSubModelKeyFilters();

                // partnumber parametresi varsa ve productSearch boşsa, partnumber'ı Search'e ekle
                var searchQuery = productSearch;
                if (!string.IsNullOrWhiteSpace(partnumber) && string.IsNullOrWhiteSpace(searchQuery))
                {
                    searchQuery = partnumber;
                }

                var result = await ProductService.GetByFilterPagingAsync(new SearchFilterReguestDto{
                        CategoryIds = SelectedCategoryIds,
                        BrandIds = SelectedBrandIds,
                        CategoryId = catid,
                        BrandId = brandid,
                        Page = page,
                        PageSize = pageSize,
                        Search = searchQuery,
                        Sort = _productFilter,
                        ManufacturerKey = manufacturerkey,
                        BaseModelKey = modelkey,
                        ModelId = modelid,
                        PartNumber = partnumber,
                        DotPartNames = SelectedDotPartNames.ToList(),
                        ManufacturerNames = SelectedManufacturerNames.ToList(),
                        BaseModelNames = SelectedBaseModelNames.ToList(),
                        SubModelNames = subModelNamesFilter,
                        SubModelKeys = subModelKeysFilter,
                        OnlyPerfectCompatibility = ShouldApplyPerfectCompatibility,
                        OnlyInStock = onlyInStockFilter,
                        SingleManufacturerName = manufacturername,
                        SingleModelName = modelname,
                        SingleSubModelName = submodelname
                    }
            );
                if(result.Ok && result.Result is not null){
                    Products = new List<SellerProductViewModel>(result.Result.Data);
                    totalCount = result.Result.DataCount;
                }
            } catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore the error
                return;
            } catch (Exception ex)
            {
                // Log other exceptions if needed
                Console.WriteLine($"Error in GetProductService: {ex.Message}");
            }
        }, "Ürünler yükleniyor");
    }
    private async Task OnSearchInput(ChangeEventArgs e)
    {
        productSearch = e.Value?.ToString() ?? string.Empty;
        page = 1; 

        if (productSearch.Length >= 3)
        {
            await GetProductService(); 
        }
        else
        {
            await LoadProducts(); 
        }
    }
    private async Task ClearSearch()
    {
        productSearch = string.Empty;
        page = 1;
        await LoadProducts();
        await RequestRender();
    }
    private static string NormalizeFriendlyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var decoded = Uri.UnescapeDataString(name.Trim());

        if (!decoded.Contains(' ') && decoded.Contains('-'))
        {
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            decoded = textInfo.ToTitleCase(decoded.Replace("-", " "));
        }

        return decoded;
    }
    private async Task CleanFilter(){
        SelectedBrandIds.Clear();
        SelectedCategoryIds.Clear();
        SelectedDotPartNames.Clear();
        SelectedManufacturerNames.Clear();
        SelectedBaseModelNames.Clear();
        SelectedSubModelNames.Clear();
        searchCategory = string.Empty;
        searchBrand = string.Empty;
        searchDotPart = string.Empty;
        searchManufacturer = string.Empty;
        searchBaseModel = string.Empty;
        searchSubModel = string.Empty;
        category.Clear();
        brands.Clear();
        dotPartNames.Clear();
        manufacturerNames.Clear();
        baseModelNames.Clear();
        subModelNames.Clear();
        CloseMobileFilter();
        await LoadProducts();
    }
    private async Task UpdateSelection(HashSet<string> set, string value, bool isChecked)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (isChecked)
        {
            set.Add(value);
        }
        else
        {
            set.Remove(value);
        }

        await GetProductService();
    }

    private Task OnDotPartFilterChanged(string value, bool isChecked)
        => UpdateSelection(SelectedDotPartNames, value, isChecked);

    private Task OnManufacturerFilterChanged(string value, bool isChecked)
        => UpdateSelection(SelectedManufacturerNames, value, isChecked);

    private Task OnBaseModelFilterChanged(string value, bool isChecked)
        => UpdateSelection(SelectedBaseModelNames, value, isChecked);

    private Task OnSubModelFilterChanged(string value, bool isChecked)
        => UpdateSelection(SelectedSubModelNames, value, isChecked);

    private async Task RemoveChip(FilterChip chip)
    {
        switch (chip.Type)
        {
            case ChipTypeCategory when chip.IntValue.HasValue:
                SelectedCategoryIds.Remove(chip.IntValue.Value);
                break;
            case ChipTypeBrand when chip.IntValue.HasValue:
                SelectedBrandIds.Remove(chip.IntValue.Value);
                break;
            case ChipTypeDotPart when !string.IsNullOrWhiteSpace(chip.StringValue):
                SelectedDotPartNames.Remove(chip.StringValue);
                break;
            case ChipTypeManufacturer when !string.IsNullOrWhiteSpace(chip.StringValue):
                SelectedManufacturerNames.Remove(chip.StringValue);
                break;
            case ChipTypeBaseModel when !string.IsNullOrWhiteSpace(chip.StringValue):
                SelectedBaseModelNames.Remove(chip.StringValue);
                break;
            case ChipTypeSubModel when !string.IsNullOrWhiteSpace(chip.StringValue):
                SelectedSubModelNames.Remove(chip.StringValue);
                break;
        }

        await GetProductService();
    }

    private async Task ToggleMobileFilter()
    {
        IsMobileFilterOpen = !IsMobileFilterOpen;
        await RequestRender();
    }

    private async Task CloseMobileFilter()
    {
        if (!IsMobileFilterOpen)
        {
            return;
        }

        IsMobileFilterOpen = false;
        await RequestRender();
    }

    private sealed class FilterChip
    {
        public FilterChip(string type, string label, int? intValue = null, string? stringValue = null)
        {
            Type = type;
            Label = label;
            IntValue = intValue;
            StringValue = stringValue;
        }

        public string Type { get; }
        public string Label { get; }
        public int? IntValue { get; }
        public string? StringValue { get; }
    }

    private async Task OnSortSelected(int val)
    {
        selectedSort = val;
        _productFilter = (ProductFilter)val;
        await GetProductService();
    }
    private IEnumerable<CategoryDto> FilteredCategories => string.IsNullOrWhiteSpace(searchCategory) || searchCategory.Length < 2 ? category : category.Where(c => c.Name.Contains(searchCategory, StringComparison.OrdinalIgnoreCase));
    private IEnumerable<BrandDto> FilteredBrands => string.IsNullOrWhiteSpace(searchBrand) || searchBrand.Length < 2 ? brands : brands.Where(b => b.Name.Contains(searchBrand, StringComparison.OrdinalIgnoreCase));
    private IEnumerable<string> FilteredDotParts => string.IsNullOrWhiteSpace(searchDotPart) || searchDotPart.Length < 2 ? dotPartNames : dotPartNames.Where(d => d.Contains(searchDotPart, StringComparison.OrdinalIgnoreCase));
    private IEnumerable<string> FilteredManufacturers => string.IsNullOrWhiteSpace(searchManufacturer) || searchManufacturer.Length < 2 ? manufacturerNames : manufacturerNames.Where(v => v.Contains(searchManufacturer, StringComparison.OrdinalIgnoreCase));
    private IEnumerable<string> FilteredBaseModels => string.IsNullOrWhiteSpace(searchBaseModel) || searchBaseModel.Length < 2 ? baseModelNames : baseModelNames.Where(b => b.Contains(searchBaseModel, StringComparison.OrdinalIgnoreCase));
    private IEnumerable<string> FilteredSubModels => string.IsNullOrWhiteSpace(searchSubModel) || searchSubModel.Length < 2 ? subModelNames : subModelNames.Where(s => s.Contains(searchSubModel, StringComparison.OrdinalIgnoreCase));
    
    public async ValueTask DisposeAsync() {
        _isDisposed = true;
        _appStateManager.StateChanged -= AppState_StateChanged;
        
        try {
            _renderCts?.Cancel();
            _renderCts?.Dispose();
        } catch { }

        try {
            // Cleanup IntersectionObserver
            if (dotNetRef != null) {
                await _jsRuntime.InvokeVoidAsync("cleanupInfiniteScroll");
                dotNetRef.Dispose();
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"⚠️ Dispose failed: {ex.Message}");
        }
    }
}
