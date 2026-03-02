using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Modal.Services;
using ecommerce.Core.Dtos;
using ecommerce.Core.Identity;
using ecommerce.Domain.Shared.Dtos.Brand;
using ecommerce.Domain.Shared.Dtos.Category;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Services;
using ecommerce.Web.Events;
using ecommerce.Web.Utility;
using ecommerce.Web.Components.Modals;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using System;
using System.Linq;
namespace ecommerce.Web.Components.Layout;
public partial class HeaderMenu{
    private bool isCategoryMenuOpen = false;
    private HashSet<int> openCategories = new();
    private string activeTab = "products";
    private List<CompatibleVehicleInfo> compatibleVehicles = new();
    [Parameter] [SupplyParameterFromQuery] public int ? CategoryId{get;set;}
    [CascadingParameter] public IModalService? _openModal { get; set; }
    [Inject] private ILocalStorageService _localStorageService{get;set;}
    [Inject] private AppStateManager _appStateManager{get;set;}
    [Inject] private II18N lang{get;set;}
    [Inject] private ICartService _cartService{get;set;}
    [Inject] private IJSRuntime _jsruntime{get;set;}
    [Inject] private ICookieManager _cookieManager{get;set;}
    [Inject] private NavigationManager _navigation{get;set;}
    [Inject] private DialogService _dialogService{get;set;}
    [Inject] private IHttpContextAccessor HttpContextAccessor{get;set;}
    [Inject] private ICategoryService _categoryService{get;set;}
    [Inject] private ISellerProductService _productSearchService{get;set;}
    [Inject] private IBrandService _brandSearchService{get;set;}
    [Inject] private ecommerce.Web.Domain.Services.IManufacturerCacheService _manufacturerService{get;set;}

    [Inject] private ecommerce.Domain.Shared.Dtos.Options.CdnOptions CdnConfig{get;set;}
    private List<CategoryElasticDto> categories = new();
    private List<BrandElasticDto> brands = new();
    private List<Domain.Dtos.ManufacturerElasticDto> manufacturers = new();
    private readonly Dictionary<int, List<Domain.Dtos.BaseModelDto>> manufacturerModelCache = new();
    private bool showBrandFlyout = false;
    private int? hoveredManufacturerId = null;
    private Domain.Dtos.ManufacturerElasticDto? activeManufacturer;
    private List<Domain.Dtos.BaseModelDto> selectedManufacturerModels = new();
    private List<Domain.Dtos.BaseModelDto> allManufacturerModels = new();
    private List<Domain.Dtos.BaseModelDto> filteredModels = new();
    private Domain.Dtos.BaseModelDto? activeBaseModel;
    private List<Domain.Dtos.SubModelDto> activeBaseModelSubModels = new();
    private List<Domain.Dtos.SubModelDto> filteredActiveSubModels = new();
    private string subModelSearchText = string.Empty;
    private bool isSubModelPanelVisible = false;
    private bool modelSearchNoResult = false;
    private string modelSearchText = "";
    private int displayCount = 50; // Server-side paging
    private bool IsLogin = false;
    private bool loginChecked = false;
    private string userInitials;
    private string Fullname;
    private bool isSearchOpen = false;
    private string searchText = "";
    private bool showOnlyInStock = false;
    private List<SellerProductViewModel> searchResults = new();
    private AuthenticationState _authenticationState;
    // Advanced Search modal reference to prevent duplicate openings
    private IModalReference? _advancedSearchModal;
    private Domain.Dtos.Cart.CartDto CartResult = new();
    private bool isCartDropdownOpen = false;
    private bool isMobile = false;
    protected override async Task OnAfterRenderAsync(bool firstRender){
        if(firstRender){
        
            isSearchOpen = false;
            isCartDropdownOpen = false;
            isCategoryMenuOpen = false;
            
            _authenticationState = await _appStateManager.GetAuthenticationStateAsync();
            if(_authenticationState.User.IsAuthenticated()){
                if(_authenticationState.User.GetUserId() > 0){
                    IsLogin = true;
                    userInitials = _authenticationState.User.FindFullName();
                    StateHasChanged();
                } else{
                    IsLogin = false;
                }
                loginChecked = true;
            }
            


            isMobile = await _jsruntime.InvokeAsync<bool>("eval", "window.innerWidth <= 767");
            
          
            var localLanguage = await _jsruntime.InvokeAsync<string>("localStorage.getItem", "lang");
            if(!string.IsNullOrEmpty(localLanguage) && lang?.Languages?.Any() == true){
                var selectedLang = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
                if(selectedLang != null){
                    lang.Language = selectedLang;
                    _appStateManager.InvokeLanguageChanged(localLanguage);
                    StateHasChanged();
                    
                }
            }
            

            if(!isMobile && manufacturers != null && manufacturers.Any())
            {
                try
                {
                    await _jsruntime.InvokeVoidAsync("initManufacturerSlider");
                }
                catch (JSDisconnectedException)
                {
                    // Ignore: circuit disconnected during hot-reload or navigation
                }
                catch (TaskCanceledException)
                {
                    // Ignore benign cancellation when component is disposing
                }
                catch (OperationCanceledException)
                {
                    // Ignore benign cancellation when component is disposing
                }
                catch (ObjectDisposedException)
                {
                    // Ignore: runtime disposed during teardown
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"⚠️ Manufacturer slider init (non-critical): {ex.Message}");
                }
            }
            
            CartResult = await _appStateManager.GetCart();
        }
        // Ensure slider init on subsequent renders as well (safe due to JS guard)
        await EnsureManufacturerSliderInitialized();
    }
    protected override async Task OnInitializedAsync(){
      
        isSearchOpen = false;
        isCartDropdownOpen = false;
        isCategoryMenuOpen = false;
        
        _navigation.LocationChanged += OnLocationChanged;
        // Ensure header listens cart updates asap
        _appStateManager.StateChanged += HandleCartStateChanged;
        
        // Load all data in parallel for faster initial load
        var cartTask = LoadCartAsync();
        var categoriesTask = LoadCategoriesAsync();
        var brandsTask = LoadBrandsAsync();
        var manufacturersTask = LoadManufacturersAsync();
        
        await Task.WhenAll(cartTask, categoriesTask, brandsTask, manufacturersTask);
    }
    
    private async Task LoadCartAsync()
    {
        try{
            CartResult = await _appStateManager.GetCart();
            if(CartResult == null || (CartResult.Sellers?.Any() != true && CartResult.CartCount == 0)){
                await _appStateManager.UpdatedCart(this, null);
                CartResult = await _appStateManager.GetCart();
            }
        } catch{
            /* ignore */
        }
    }
    
    private async Task LoadCategoriesAsync()
    {
        var response = await _categoryService.GetAllAsync();
        if(response.Ok && response.Result is not null){
            categories = response.Result;
        }
    }
    
    private async Task LoadBrandsAsync()
    {
        var brandResult = await _brandSearchService.SearchAsync(searchText);
        if(brandResult.Ok && brandResult.Result != null){
            brands = brandResult.Result;
        }
    }
    
    private async Task LoadManufacturersAsync()
    {
        try
        {
            var manufacturersResult = await _manufacturerService.GetAllAsync();
            if(manufacturersResult.Ok && manufacturersResult.Result != null)
            {
                // Tekrar eden markaları Id ve İsme göre temizle (çift güvenlik)
                manufacturers = manufacturersResult.Result
                    .DistinctBy(m => m.Id) // İlk olarak Id'ye göre
                    .DistinctBy(m => m.Name?.Trim().ToLowerInvariant()) // Sonra isme göre (büyük/küçük harf duyarsız)
                    .ToList();
                    
                foreach(var manufacturer in manufacturers)
                {
                    if(manufacturer.Models != null && manufacturer.Models.Any())
                    {
                        manufacturerModelCache[manufacturer.Id] = manufacturer.Models;
                    }
                }
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"❌ Üreticiler yüklenirken hata: {ex.Message}");
        }
    }

    // Ensure manufacturer slider is initialized after any re-render or navigation
    private bool _isDisposed = false;
    private async Task EnsureManufacturerSliderInitialized()
    {
        try
        {
            if(!_isDisposed && !isMobile && manufacturers != null && manufacturers.Any())
            {
                await _jsruntime.InvokeVoidAsync("initManufacturerSlider");
            }
        }
        catch (JSDisconnectedException)
        {
            // Safe to ignore during disconnect
        }
        catch (TaskCanceledException)
        {
            // Safe to ignore during cancellation
        }
        catch (OperationCanceledException)
        {
            // Safe to ignore during cancellation
        }
        catch (ObjectDisposedException)
        {
            // Safe to ignore if runtime/component disposed
        }
        catch(Exception ex)
        {
            Console.WriteLine($"⚠️ EnsureManufacturerSliderInitialized (non-critical): {ex.Message}");
        }
    }

    private async void HandleCartStateChanged(ComponentBase source, string evt, Domain.Dtos.Cart.CartDto? updatedCart)
    {
        if(evt != AppStateEvents.updateCart){
            return;
        }
        if(ReferenceEquals(source, this)){
            return;
        }
        CartResult = updatedCart ?? CartResult ?? await _appStateManager.GetCart();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if(_isDisposed) return;
        _isDisposed = true;
        try{
            _appStateManager.StateChanged -= HandleCartStateChanged;
        } catch { }
        try{
            _navigation.LocationChanged -= OnLocationChanged;
        } catch { }
    }
    private async Task SetLanguage(string locale){
        try{
            await _jsruntime.ShowFullPageLoader();
            lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == locale);
            _appStateManager.InvokeLanguageChanged(locale);
            await _jsruntime.SetInLocalStorage("lang", lang.Language!.Locale);
            StateHasChanged();
            var uri = new Uri(_navigation.Uri).GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
            _navigation.NavigateTo(uri, forceLoad:true);
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        } finally{
            await _jsruntime.HideFullPageLoader();
        }
    }
    // private string GetInitials(string fullName)
    // {
    //     if (string.IsNullOrWhiteSpace(fullName))
    //         return string.Empty;
    //
    //     var names = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //     return names.Length switch
    //     {
    //         0 => string.Empty,
    //         1 => names[0][0].ToString().ToUpper(),
    //         _ => $"{names[0][0]}{names[^1][0]}".ToUpper() 
    //     };
    // }
    private async Task Logout(){
        try{
            var confirmed = await _dialogService.Confirm(@lang["LogoutInfo"], @lang["LogoutInfoSur"], new ConfirmOptions{OkButtonText = @lang["LogoutYes"], CancelButtonText = @lang["LogoutNo"],});
            if(confirmed != true) return;

        
            try{ await _jsruntime.InvokeVoidAsync("fetch", "/auth/logout", new { method = "POST" }); } catch { }

            // 2) Client tokens cleanup if any stored
            try{ await _jsruntime.InvokeVoidAsync("localStorage.removeItem", "access_token"); } catch { }
            try{ await _jsruntime.InvokeVoidAsync("localStorage.removeItem", "refresh_token"); } catch { }


            await _appStateManager.UpdatedCart(this, null);
            IsLogin = false;

            // 4) Redirect
            _navigation.NavigateTo("/login", forceLoad:true);
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
    }
    private void CloseSearch(){
        isSearchOpen = false;
        StateHasChanged();
    }
    private async Task OnSearchInput(ChangeEventArgs e){
        searchText = e.Value?.ToString() ?? "";
        if(!string.IsNullOrEmpty(searchText) && searchText.Trim().Length >= 3){
            // Diğer açık menüleri kapat
            if (isCartDropdownOpen)
            {
                isCartDropdownOpen = false;
            }
            if (isCategoryMenuOpen)
            {
                isCategoryMenuOpen = false;
            }
            
            await SearchProducts();
            isSearchOpen = true;
        } else{
            isSearchOpen = false;
            searchResults.Clear();
        }
        StateHasChanged();
    }
    private async Task SearchProducts(){
        try{
            var productResponse = await _productSearchService.SearchAsync(searchText, showOnlyInStock);
            if(productResponse.Ok && productResponse.Result != null){
                searchResults = productResponse.Result;
                
                // Uyumlu araçları topla - artık gerçek sayıları backend'den alıyor
                await ExtractCompatibleVehicles(searchResults);
            } else{
                searchResults.Clear();
                compatibleVehicles.Clear();
            }
            var brandResponse = await _brandSearchService.SearchAsync(searchText);
            if(brandResponse.Ok && brandResponse.Result != null){
                brands = brandResponse.Result;
            } else{
                brands.Clear();
            }
        } catch(Exception ex){
            Console.WriteLine($"Search error: {ex.Message}");
            searchResults.Clear();
            brands.Clear();
            compatibleVehicles.Clear();
        }
    }
    
    private async Task ExtractCompatibleVehicles(List<SellerProductViewModel> products)
    {
        try
        {
            compatibleVehicles.Clear();
            var vehicleDict = new Dictionary<string, (CompatibleVehicleInfo info, HashSet<int> productIds)>();
            
            if(products == null) return;
            
            foreach(var product in products)
            {
                if(product == null || product.SubModelsJson == null || !product.SubModelsJson.Any())
                    continue;
                    
                foreach(var subModel in product.SubModelsJson)
                {
                    if(subModel == null || string.IsNullOrWhiteSpace(subModel.Name))
                        continue;
                        
                    var manufacturerName = product.ManufacturerName ?? "";
                    var baseModelName = product.BaseModelName ?? "";
                    var subModelName = subModel.Name ?? "";
                    var manufacturerKey = product.ManufacturerKey;
                    var baseModelKey = product.BaseModelKey;
                    var subModelKey = subModel.Key;
                        
                    var key = $"{manufacturerName}_{baseModelName}_{subModelName}";
                    if(!vehicleDict.ContainsKey(key))
                    {
                        vehicleDict[key] = (
                            new CompatibleVehicleInfo
                            {
                                ManufacturerName = manufacturerName,
                                BaseModelName = baseModelName,
                                SubModelName = subModelName,
                                ManufacturerKey = manufacturerKey,
                                BaseModelKey = baseModelKey,
                                SubModelKey = subModelKey,
                                ProductCount = 0
                            },
                            new HashSet<int>());
                    }
                    
                    // Her ürün sadece bir kez sayılmalı
                    vehicleDict[key].productIds.Add(product.ProductId);
                }
            }
            
            // HashSet'teki benzersiz ürün sayısını ProductCount'a aktar
            compatibleVehicles = vehicleDict.Values
                .Select(v =>
                {
                    v.info.ProductCount = v.productIds.Count;
                    return v.info;
                })
                .Where(v => v.ProductCount > 0)
                .OrderByDescending(v => v.ProductCount)
                .ThenBy(v => v.ManufacturerName)
                .ThenBy(v => v.BaseModelName)
                .ToList();
                
            Console.WriteLine($"✅ Uyumlu araçlar: {compatibleVehicles.Count} adet bulundu.");
            
            await Task.CompletedTask; // Async uyumluluğu için
        }
        catch(Exception ex)
        {
            Console.WriteLine($"❌ ExtractCompatibleVehicles hatası: {ex.Message}");
        }
    }    
    private class CompatibleVehicleInfo
    {
        public string ManufacturerName { get; set; } = "";
        public string BaseModelName { get; set; } = "";
        public string SubModelName { get; set; } = "";
        public string? ManufacturerKey { get; set; }
        public string? BaseModelKey { get; set; }
        public string? SubModelKey { get; set; }
        public int ProductCount { get; set; }
    }
    private void SwitchTab(string tab){activeTab = tab;}
    private void GoToProduct(int productId){
        isSearchOpen = false;
        searchText = "";
        searchResults.Clear();
        _navigation.NavigateTo($"/product-detail?productId={productId}");
    }
    
    private void GoToVehicle(CompatibleVehicleInfo vehicle){
        isSearchOpen = false;
        var encodedSearch = Uri.EscapeDataString(searchText);
        searchText = "";
        searchResults.Clear();
        compatibleVehicles.Clear();
        
        // IMPORTANT: Dropdown counts products from SEARCH RESULTS that match this vehicle
        // So we should use the SAME search query, not add vehicle names
        // The dropdown shows: "egea filitre" → 15 products match Egea
        // We should navigate with: "egea filitre" (same query)
        // NOT: "egea filitre Fiat Egea" (too specific!)
        
        var targetUrl = $"/product-search?query={encodedSearch}";
        _navigation.NavigateTo(targetUrl);
    }
    
    private void OnLocationChanged(object ? sender, LocationChangedEventArgs e){
        isSearchOpen = false;
        isCartDropdownOpen = false; 
        searchText = "";
        searchResults.Clear();

        _ = Task.Run(async () => {
            try
            {
                if(manufacturers == null || manufacturers.Count == 0)
                {
                    var manufacturersResult = await _manufacturerService.GetAllAsync();
                    if(manufacturersResult.Ok && manufacturersResult.Result != null)
                    {
                        // Tekrar eden markaları Id ve İsme göre temizle (çift güvenlik)
                        manufacturers = manufacturersResult.Result
                            .DistinctBy(m => m.Id) // İlk olarak Id'ye göre
                            .DistinctBy(m => m.Name?.Trim().ToLowerInvariant()) // Sonra isme göre (büyük/küçük harf duyarsız)
                            .ToList();
                    }
                }
            }
            catch { /* ignore network errors */ }
            finally
            {
                await EnsureManufacturerSliderInitialized();
                await InvokeAsync(StateHasChanged);
            }
        });
    }
    private void RedirectToSearch(){
        if(!string.IsNullOrWhiteSpace(searchText) && searchText.Trim().Length >= 3){
            var encodedSearch = Uri.EscapeDataString(searchText);
            _navigation.NavigateTo($"/product-search?query={encodedSearch}");
            isSearchOpen = false;
            searchText = "";
            searchResults.Clear();
        }
    }
    private Task OnSearchKeyDown(KeyboardEventArgs e){
        if(e.Key == "Escape" && isSearchOpen){
            CloseSearch();
            return Task.CompletedTask;
        }
        if(e.Key == "Enter")
        {
            return OnSearchSubmitAsync();
        }
        return Task.CompletedTask;
    }

    private async Task OnOnlyInStockToggle(ChangeEventArgs e)
    {
        showOnlyInStock = (bool)(e.Value ?? false);
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            await SearchProducts();
        }
    }

    private async Task OnSearchSubmitAsync()
    {
        if(string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 3)
        {
            return;
        }

        RedirectToSearch();
        await Task.CompletedTask;
    }
    private void ClearSearch(){
        searchText = "";
        searchResults.Clear();
        isSearchOpen = false;
    }
    private static bool HasPerfectCompatibility(SellerProductViewModel? product)
        => product?.PerfectCompatibilityCars?.Any() == true;

    private static string GetCompatibilitySummary(SellerProductViewModel? product)
    {
        if(product?.PerfectCompatibilityCars == null || product.PerfectCompatibilityCars.Count == 0){
            return string.Empty;
        }

        var car = product.PerfectCompatibilityCars.FirstOrDefault();
        if(car == null){
            return string.Empty;
        }

        var parts = new List<string>();
        if(!string.IsNullOrWhiteSpace(car.ManufacturerName)) parts.Add(car.ManufacturerName.Trim());
        if(!string.IsNullOrWhiteSpace(car.BaseModelName)) parts.Add(car.BaseModelName.Trim());
        if(!string.IsNullOrWhiteSpace(car.SubModelName)) parts.Add(car.SubModelName.Trim());
        if(!string.IsNullOrWhiteSpace(car.PlateNumber)) parts.Add($"({car.PlateNumber.Trim()})");

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
    }

    private string ResolveSearchImage(SellerProductViewModel product)
    {
        var firstImage = product.Images?.FirstOrDefault();
        if(firstImage?.FileGuid != null){
            return $"{CdnConfig.BaseUrl}/ProductImages/{firstImage.FileGuid}";
        }
        if(firstImage?.FileName != null){
            return $"{CdnConfig.BaseUrl}/ProductImages/{firstImage.FileName}";
        }
        return "assets/images/product/category/4.jpg";
    }
    private void ToggleCategoryMenu(){
       
        if (isSearchOpen)
        {
            isSearchOpen = false;
        }
        if (isCartDropdownOpen)
        {
            isCartDropdownOpen = false;
        }
        
        isCategoryMenuOpen = !isCategoryMenuOpen;
        StateHasChanged();
    }
    private async Task OpenCategoryMenu()
    {
        isCategoryMenuOpen = true;
        await InvokeAsync(StateHasChanged);
    }
    private void CloseCategoryMenu(){
        isCategoryMenuOpen = false;
        openCategories.Clear();
        StateHasChanged();
    }

    
    private void ToggleCategory(int categoryId){
        if(openCategories.Contains(categoryId)){
            openCategories.Remove(categoryId);
        } else{
            openCategories.Add(categoryId);
        }
        StateHasChanged();
    }
    
    private void ToggleCartDropdown(){
       
        if (isSearchOpen)
        {
            isSearchOpen = false;
        }
        if (isCategoryMenuOpen)
        {
            isCategoryMenuOpen = false;
        }
        
        isCartDropdownOpen = !isCartDropdownOpen;
        StateHasChanged();
    }
    
    private void CloseCartDropdown(){
        isCartDropdownOpen = false;
        StateHasChanged();
    }

    private void OpenAdvancedSearch(bool closeSearchFirst = false)
    {
        if(closeSearchFirst)
        {
            isSearchOpen = false;
            StateHasChanged();
        }

        if(_openModal == null)
        {
            return;
        }

        // Prevent opening multiple AdvancedSearchModal instances
        if (_advancedSearchModal != null)
        {
            return;
        }

        var parameters = new ModalParameters();
        parameters.Add(nameof(AdvancedSearchModal.Manufacturers), manufacturers ?? new List<Domain.Dtos.ManufacturerElasticDto>());
        parameters.Add(nameof(AdvancedSearchModal.ManufacturerModelCache), manufacturerModelCache);

        var options = new ModalOptions
        {
           
            DisableBackgroundCancel = false,
            HideHeader = true,
            Size = ModalSize.Large,
            HideCloseButton = true,
            AnimationType = ModalAnimationType.FadeInOut
        };

        var title = lang["Search.AdvancedTitle"] ?? "Advanced Search";
        _advancedSearchModal = _openModal.Show<AdvancedSearchModal>(title, parameters, options);
        _ = TrackAdvancedSearchModalAsync(_advancedSearchModal);
    }

    private async Task TrackAdvancedSearchModalAsync(IModalReference modalRef)
    {
        try
        {
            await modalRef.Result;
        }
        finally
        {
            _advancedSearchModal = null;
        }
    }
    private async Task RemoveFromCart(int cartItemId){
        try{
            if(CartResult?.Sellers != null){
                foreach(var seller in CartResult.Sellers){
                    seller.Items.RemoveAll(i => i.Id == cartItemId);
                }
                CartResult.Sellers.RemoveAll(s => s.Items == null || !s.Items.Any());
                CartResult.TotalItems = CartResult.Sellers.SelectMany(s => s.Items).Sum(i => i.Quantity);
                CartResult.CartCount = CartResult.TotalItems;
                CartResult.OrderTotal = CartResult.Sellers.SelectMany(s => s.Items).Sum(i => i.Quantity * i.UnitPrice);
                await InvokeAsync(StateHasChanged);
            }
            var result = await _cartService.CartItemRemove(cartItemId);
            if(result.Ok){
                await Task.Delay(50);
                await _appStateManager.UpdatedCart(this, null);
                CartResult = await _appStateManager.GetCart();
                await InvokeAsync(StateHasChanged);
            } else{
                CartResult = await _appStateManager.GetCart();
                await InvokeAsync(StateHasChanged);
            }
        } catch(Exception ex){
            Console.WriteLine($"Error removing cart item: {ex.Message}");
            await _appStateManager.UpdatedCart(this, null);
            CartResult = await _appStateManager.GetCart();
            await InvokeAsync(StateHasChanged);
        }
    }

    private string GetCategoryIcon(string categoryName)
    {
        var name = categoryName.ToLower();
        return name switch
        {
            var n when n.Contains("motor") => "fa-solid fa-cogs",
            var n when n.Contains("elektrik") => "fa-solid fa-bolt",
            var n when n.Contains("süspansiyon") || n.Contains("suspansiyon") => "fa-solid fa-car-side",
            var n when n.Contains("fren") => "fa-solid fa-circle-stop",
            var n when n.Contains("soğutma") || n.Contains("sogutma") => "fa-solid fa-snowflake",
            var n when n.Contains("iç") || n.Contains("ic") => "fa-solid fa-chair",
            var n when n.Contains("dış") || n.Contains("dis") => "fa-solid fa-car",
            var n when n.Contains("yağ") || n.Contains("yag") => "fa-solid fa-tint",
            var n when n.Contains("filtre") => "fa-solid fa-filter",
            var n when n.Contains("akü") || n.Contains("aku") => "fa-solid fa-battery-full",
            var n when n.Contains("lastik") => "fa-solid fa-circle",
            var n when n.Contains("jant") => "fa-solid fa-circle-notch",
            var n when n.Contains("klima") => "fa-solid fa-wind",
            var n when n.Contains("egzoz") => "fa-solid fa-smoke",
            _ => "fa-solid fa-cog"
        };
    }
    
    private async Task OpenBrandFlyout(int manufacturerId)
    {
        try
        {
            if(hoveredManufacturerId == manufacturerId && showBrandFlyout)
                return; // Zaten açık
            
            hoveredManufacturerId = manufacturerId;
            modelSearchText = "";
            modelSearchNoResult = false;
            displayCount = 50;
            ResetActiveModel();
            
            // Get manufacturer name first
            var manufacturer = manufacturers.FirstOrDefault(m => m.Id == manufacturerId);
            if (manufacturer == null) return;
            activeManufacturer = manufacturer;

            if (manufacturerModelCache.TryGetValue(manufacturerId, out var cachedModels) && cachedModels.Any())
            {
                allManufacturerModels = cachedModels
                    .OrderBy(m => m.VehicleType == 1 ? 0 : 1)
                    .ThenBy(m => string.IsNullOrEmpty(m.ImageUrl) ? 1 : 0)
                    .ThenBy(m => m.Name)
                    .ToList();
                manufacturer.Models = allManufacturerModels;
                manufacturer.ModelCount = allManufacturerModels.Count;
                filteredModels = allManufacturerModels;
                selectedManufacturerModels = allManufacturerModels.Take(displayCount).ToList();
                modelSearchNoResult = false;
                showBrandFlyout = true;
                if (selectedManufacturerModels.Any())
                {
                    var preferred = GetPreferredModel(selectedManufacturerModels);
                    if (preferred != null)
                    {
                        ShowModelDetails(preferred);
                    }
                }
                await InvokeAsync(StateHasChanged);
                return;
            }
            
            // Get ALL models for this manufacturer (across all VehicleTypes)
            var result = await _manufacturerService.GetByNameAsync(manufacturer.Name);
            if(result.Ok && result.Result != null && result.Result.Models.Any())
            {
                allManufacturerModels = result.Result.Models
                    .OrderBy(m => m.VehicleType == 1 ? 0 : 1)  // 1. Otomobil önce
                    .ThenBy(m => string.IsNullOrEmpty(m.ImageUrl) ? 1 : 0)  // 2. Fotoğraflı önce
                    .ThenBy(m => m.Name)  // 3. İsme göre
                    .ToList();
                manufacturerModelCache[manufacturer.Id] = allManufacturerModels;

                var existing = manufacturers.FirstOrDefault(x => x.Id == manufacturer.Id);
                if(existing != null)
                {
                    existing.Models = allManufacturerModels;
                    existing.ModelCount = allManufacturerModels.Count;
                }
                
                modelSearchNoResult = false;
                FilterModels();
                showBrandFlyout = true;
                if (selectedManufacturerModels.Any())
                {
                    var preferred = GetPreferredModel(selectedManufacturerModels);
                    if (preferred != null)
                    {
                        ShowModelDetails(preferred);
                    }
                }
                await InvokeAsync(StateHasChanged);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"❌ Error opening brand flyout: {ex.Message}");
            showBrandFlyout = false;
        }
    }
    
    private void GoToModel(int manufacturerId, string manufacturerName, int modelId, string modelName, string manufacturerKey, string baseModelKey)
    {
        showBrandFlyout = false;
        var encodedManufacturer = Uri.EscapeDataString(manufacturerName);
        var encodedModel = Uri.EscapeDataString(modelName);
        _navigation.NavigateTo($"/product-search?manufacturerid={manufacturerId}&manufacturername={encodedManufacturer}&manufacturerkey={manufacturerKey}&modelid={modelId}&modelkey={baseModelKey}&modelname={encodedModel}");
    }

    private void GoToManufacturer(int manufacturerId, string manufacturerName, string manufacturerKey)
    {
        showBrandFlyout = false;
        var friendlyManufacturer = FriendlyUrlHelper.GetFriendlyTitle(manufacturerName);
        _navigation.NavigateTo($"/product-search?manufacturerid={manufacturerId}&manufacturername={friendlyManufacturer}&manufacturerkey={manufacturerKey}");
    }
 
    private string GetVehicleTypeName(int vehicleType)
    {
        return vehicleType switch
        {
            1 => "Otomobil",
            2 => "Ticari Araç",
            3 => "Motosiklet",
            4 => "Kamyon",
            _ => "Diğer"
        };
    }
    
    private string GetVehicleTypeIcon(int vehicleType)
    {
        return vehicleType switch
        {
            1 => "car",
            2 => "van-shuttle",
            3 => "motorcycle",
            4 => "truck",
            _ => "car"
        };
    }
    
    private void FilterModels()
    {
        try
        {
            if(!allManufacturerModels.Any())
            {
                filteredModels = new();
                selectedManufacturerModels = new();
                modelSearchNoResult = false;
                ResetActiveModel();
                return;
            }
            
            // Search filter only
            if(!string.IsNullOrWhiteSpace(modelSearchText))
            {
                filteredModels = allManufacturerModels
                    .Where(x => x.Name.Contains(modelSearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!filteredModels.Any())
                {
                    modelSearchNoResult = true;
                    filteredModels = allManufacturerModels;
                }
                else
                {
                    modelSearchNoResult = false;
                }
            }
            else
            {
                filteredModels = allManufacturerModels;
                modelSearchNoResult = false;
            }
            
            // Server-side paging: first 50
            displayCount = 50;
            selectedManufacturerModels = filteredModels.Take(displayCount).ToList();
            SyncActiveModel();
            
            // Menu AÇIK kalsın, sonuç bulunamazsa bile
            StateHasChanged();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"❌ Error filtering models: {ex.Message}");
        }
    }
    
    private void LoadMore()
    {
        displayCount += 50;
        selectedManufacturerModels = filteredModels.Take(displayCount).ToList();
        SyncActiveModel();
        StateHasChanged();
    }

    private void ShowModelDetails(Domain.Dtos.BaseModelDto model)
    {
        if (activeBaseModel != null && activeBaseModel.Id == model.Id && activeBaseModel.SubModels?.Count == model.SubModels?.Count)
        {
            subModelSearchText = string.Empty;
            FilterActiveSubModels();
            StateHasChanged();
            return;
        }

        activeBaseModel = model;
        subModelSearchText = string.Empty;
        activeBaseModelSubModels = (model.SubModels ?? new List<Domain.Dtos.SubModelDto>())
            .OrderBy(sm => sm.Name)
            .ToList();
        FilterActiveSubModels();
        isSubModelPanelVisible = true;
        InvokeAsync(async () => await LoadSubModelsFromCacheAsync(model));
        StateHasChanged();
    }

    private async Task LoadSubModelsFromCacheAsync(Domain.Dtos.BaseModelDto model)
    {
        try
        {
            if (activeManufacturer == null)
            {
                return;
            }

            var rs = await _manufacturerService.GetByIdAsync(activeManufacturer.Id);
            if (rs.Ok && rs.Result != null)
            {
                var full = rs.Result;
                var found = (full.Models ?? new List<Domain.Dtos.BaseModelDto>())
                    .FirstOrDefault(m => m.Id == model.Id)
                    ?? (full.Models ?? new List<Domain.Dtos.BaseModelDto>()).FirstOrDefault(m => m.BaseModelKey == model.BaseModelKey);

                var fetched = (found?.SubModels ?? new List<Domain.Dtos.SubModelDto>())
                    .OrderBy(x => x.Name)
                    .ToList();

                if (activeBaseModel != null && activeBaseModel.Id == model.Id)
                {
                    activeBaseModelSubModels = fetched;
                    FilterActiveSubModels();
                    isSubModelPanelVisible = true;
                    StateHasChanged();
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private void FilterActiveSubModels()
    {
        if (!activeBaseModelSubModels.Any())
        {
            filteredActiveSubModels = new List<Domain.Dtos.SubModelDto>();
            return;
        }

        if (string.IsNullOrWhiteSpace(subModelSearchText) || subModelSearchText.Trim().Length < 2)
        {
            filteredActiveSubModels = activeBaseModelSubModels;
        }
        else
        {
            var search = subModelSearchText.Trim();
            filteredActiveSubModels = activeBaseModelSubModels
                .Where(sm => sm.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private void ResetActiveModel()
    {
        activeBaseModel = null;
        activeBaseModelSubModels = new();
        filteredActiveSubModels = new();
        subModelSearchText = string.Empty;
        isSubModelPanelVisible = false;
    }

    private void SyncActiveModel()
    {
        if (!isSubModelPanelVisible)
        {
            return;
        }

        if (activeBaseModel == null)
        {
            var preferred = GetPreferredModel(selectedManufacturerModels);
            if (preferred != null)
            {
                ShowModelDetails(preferred);
            }
            else
            {
                ResetActiveModel();
            }
            return;
        }

        var matching = selectedManufacturerModels.FirstOrDefault(m => m.Id == activeBaseModel.Id);
        if (matching != null)
        {
            ShowModelDetails(matching);
        }
        else if (selectedManufacturerModels.Any())
        {
            var fallback = GetPreferredModel(selectedManufacturerModels);
            if (fallback != null)
            {
                ShowModelDetails(fallback);
            }
            else
            {
                ResetActiveModel();
            }
        }
        else
        {
            ResetActiveModel();
        }
    }

    private void UpdateSubModelSearch(ChangeEventArgs e)
    {
        subModelSearchText = e.Value?.ToString() ?? string.Empty;
        FilterActiveSubModels();
        StateHasChanged();
    }

    private void GoToSubModel(Domain.Dtos.ManufacturerElasticDto manufacturer, Domain.Dtos.BaseModelDto model, Domain.Dtos.SubModelDto subModel)
    {
        showBrandFlyout = false;

        var parameters = new List<string>
        {
            $"manufacturerid={manufacturer.Id}",
            $"manufacturername={Uri.EscapeDataString(manufacturer.Name ?? string.Empty)}"
        };

        if (!string.IsNullOrWhiteSpace(manufacturer.DatKey))
        {
            parameters.Add($"manufacturerkey={Uri.EscapeDataString(manufacturer.DatKey)}");
        }

        if (model.Id > 0)
        {
            parameters.Add($"modelid={model.Id}");
        }

        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            parameters.Add($"modelname={Uri.EscapeDataString(model.Name)}");
        }

        if (!string.IsNullOrWhiteSpace(model.BaseModelKey))
        {
            parameters.Add($"modelkey={Uri.EscapeDataString(model.BaseModelKey)}");
        }

        if (!string.IsNullOrWhiteSpace(subModel.Name))
        {
            parameters.Add($"submodelname={Uri.EscapeDataString(subModel.Name)}");
        }

        if (!string.IsNullOrWhiteSpace(subModel.SubModelKey))
        {
            parameters.Add($"submodelkey={Uri.EscapeDataString(subModel.SubModelKey)}");
        }

        var targetUrl = $"/product-search?{string.Join("&", parameters)}";
        _navigation.NavigateTo(targetUrl);
    }

    private Domain.Dtos.BaseModelDto? GetPreferredModel(IEnumerable<Domain.Dtos.BaseModelDto> models)
    {
        if (models == null)
        {
            return null;
        }

        return models.FirstOrDefault(m => m.SubModels?.Any() == true)
            ?? models.FirstOrDefault();
    }
}
