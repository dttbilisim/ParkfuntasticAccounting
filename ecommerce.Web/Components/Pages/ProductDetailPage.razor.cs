using Blazored.LocalStorage;
using ecommerce.Core.Entities;
using ecommerce.Domain.Shared.Dtos.Options;
using System.Threading;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Dtos;
using ecommerce.Web.Events;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
namespace ecommerce.Web.Components.Pages;
public partial class ProductDetailPage : IDisposable{
    private int productId;
    private int brandId;
    private int _lastProductId;
    private int _lastBrandId;
    private bool _isDisposed;
    private CancellationTokenSource? _renderCts;
    [Inject] ISellerProductService _productService{get;set;}
    [Inject] NavigationManager Navigation{get;set;}
    [Inject] CdnOptions CdnConfig{get;set;}
    [Inject] private IJSRuntime _jsRuntime{get;set;}
    [Inject] private IFavoriteService FavoriteService{get;set;}
    [Inject] private NotificationService _notificationService{get;set;}
    [Inject] private ILocalStorageService _localStorageService{get;set;}
    [Inject] private AppStateManager _appStateManager{get;set;}
    [Inject] private ICartService _cartService{get;set;}
    [Inject] private II18N lang{get;set;}
    [Inject] private IDotVehicleDataService _dotVehicleDataService{get;set;}
    [Inject] private IServiceScopeFactory _scopeFactory { get; set; }
    
    private bool _shouldRender = true;
    protected override bool ShouldRender() => _shouldRender;
    
    [Inject] private IBankService _bankService { get; set; }

    private List<ecommerce.Web.ViewModels.BankInstallmentViewModel> BankInstallments = new();
    private bool AreInstallmentsLoaded = false;
    private bool IsLoadingInstallments = false;
    private int SelectedBankId = 0;


    
    private SellerProductViewModel ? Product;
    private Domain.Dtos.Cart.CartDto CartResult = new();
    private List<SellerProductViewModel ?> brandProducts;
    private string activeTab = "features";
    private string selectedManufacturer = "";
    private List<string> manufacturers = new();
    private string searchQuery = "";
    private string selectedBaseModelKey = "";

    private void SelectModel(string key)
    {
        selectedBaseModelKey = key;
        _shouldRender = true;
    }
    private List<BaseModelDto> compatibleModels = new();
    private bool vehiclesLoaded;
    private int currentPage = 1;
    private int pageSize = 5;
    private HashSet<string> expandedModels = new HashSet<string>();
    private static readonly StringComparer CompatibilityComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly char[] GroupCodePrimarySeparators = { '|', ',', ';' };
    private static readonly char[] GroupCodeSecondarySeparators = { '-' };
    
    protected override async Task OnParametersSetAsync(){
        if (productId == _lastProductId && brandId == _lastBrandId)
            return;
            
        _lastProductId = productId;
        _lastBrandId = brandId;
        
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (_isDisposed) return;

        await _appStateManager.ExecuteWithLoading(async () => {
            var productResult = await _productService.GetByIdAsync(productId);
            if (_isDisposed) return;

            if(productResult.Ok) {
                Product = productResult.Result;
                
                compatibleModels = new List<BaseModelDto>();
                manufacturers = new List<string>();
                selectedManufacturer = "";
                searchQuery = "";
                currentPage = 1;
                expandedModels.Clear();
                _jsonLdPending = true;
            }
            var brandResult = await _productService.GetByBrandIdAsync(brandId);
            if (_isDisposed) return;
            if(brandResult.Ok) brandProducts = brandResult.Result;
            
            CartResult = await _appStateManager.GetCart();
            if (_isDisposed) return;

            if (Product?.ProductId > 0)
            {
                await LoadCompatibleVehiclesAsync(false);
            }
            
            if (_isDisposed) return;
            _shouldRender = true;
                await RequestRender();
        }, "Ürün detayları yükleniyor");
    }
    private bool _jsonLdPending;
    private async Task PushJsonLd(){
        if(Product == null) return;
        var imageUrl = Product?.Images != null && Product.Images.Any() && !string.IsNullOrEmpty(Product.Images.First().FileGuid) ? $"{CdnConfig.BaseUrl}/ProductImages/{Product.Images.First().FileGuid}" :
            Product?.Images != null && Product.Images.Any() && Product.Images.First().FileName != null ? $"{CdnConfig.BaseUrl}/ProductImages/{Product.Images.First().FileName}" :
            "assets/images/product/category/1.jpg";
        var desc = (Product?.ProductDescription ?? Product?.DotPartDescription ?? Product?.ProductName) ?? "";
        await _jsRuntime.InvokeVoidAsync("addProductJsonLd", new {
            name = Product?.ProductName,
            image = imageUrl,
            description = desc,
            brand = Product?.Brand?.Name,
            sku = Product?.ProductId.ToString(),
            url = Navigation.Uri,
            currency = "TRY",
            price = Product?.SalePrice,
            availability = (Product?.Stock > 0 ? "InStock" : "OutOfStock")
        });
        _jsonLdPushedForProductId = Product?.ProductId;
    }
    private int? _jsonLdPushedForProductId;
    protected override async Task OnInitializedAsync(){
        _appStateManager.StateChanged += AppState_StateChanged;
        Navigation.LocationChanged += OnLocationChanged;
        

        ParseQueryParameters();
        
        CartResult = await _appStateManager.GetCart();
    }
    
    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        if (_isDisposed) return;
        
        ParseQueryParameters();
        // DO NOT call OnParametersSetAsync manually. 
        // Just trigger a re-load if IDs changed.
        if (productId != _lastProductId || brandId != _lastBrandId)
        {
            _ = InvokeAsync(async () => await LoadDataAsync());
        }
    }
    
    private void ParseQueryParameters()
    {
        var uri = new Uri(Navigation.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        
        int.TryParse(query["productId"], out productId);
        int.TryParse(query["brandId"], out brandId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender){
        if(firstRender){
            try{
                if(_localStorageService != null){
                    var localLanguage = await _localStorageService.GetItemAsync<string>("lang");
                    if(!string.IsNullOrEmpty(localLanguage)){
                        _appStateManager.InvokeLanguageChanged(localLanguage);
                        lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
                    }
                CartResult = await _appStateManager.GetCart();
                _shouldRender = true;
                await RequestRender();
            }
            } catch(JSDisconnectedException){
                Console.WriteLine("⚠️ Circuit disconnected before JS call could complete.");
            }
        }
        try{
            if(Product != null && (_jsonLdPending || _jsonLdPushedForProductId != Product.ProductId)){
                await PushJsonLd();
                _jsonLdPending = false;
            }
        } catch(JSDisconnectedException){
            // ignore
        }
    }

    private async void AppState_StateChanged(ComponentBase source, string property, Domain.Dtos.Cart.CartDto? updatedCart){
        if(_isDisposed || property != AppStateEvents.updateCart){
            return;
        }

        try {
            await InvokeAsync(async () => {
                CartResult = updatedCart ?? await _appStateManager.GetCart();
                await RequestRender();
            });
        } catch { }
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
            await Task.Delay(15, token); 
            
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

    private async Task CartInsert(bool isIncrease, int step = 1, int? quantityOverride = null){
        await _appStateManager.ExecuteWithLoading(async () => {
            try{
                if(Product?.SellerItemId <= 0) return;
                
                var req = new Domain.Dtos.Cart.CartItemUpsertDto{
                    ProductSellerItemId = Product.SellerItemId,
                    Quantity = quantityOverride ?? ((isIncrease ? 1 : -1) * step)
                };
                var result = await _cartService.CreateCartItem(req);
                if(result.Ok){
                    _notificationService.Notify(NotificationSeverity.Success, result.Metadata?.Message ?? "Sepet güncellendi");
                    await _appStateManager.UpdatedCart(this, result.Result); // sunucudan dönen güncel sepeti paylaş
                    CartResult = result.Result ?? await _appStateManager.GetCart();

                    // Check for discount on the inserted item and trigger confetti
                    if (isIncrease && CartResult?.Sellers != null)
                    {
                        var insertedItem = CartResult.Sellers
                            .SelectMany(s => s.Items ?? Enumerable.Empty<Domain.Dtos.Cart.CartItemDto>())
                            .FirstOrDefault(i => i.ProductSellerItemId == Product.SellerItemId);

                        if (insertedItem != null && (insertedItem.DiscountAmount > 0 || insertedItem.AppliedDiscounts?.Any() == true))
                        {
                             try{ await _jsRuntime.InvokeVoidAsync("confettiEnsureBlast"); } catch { }
                        }
                    }

                    _shouldRender = true;
                        await RequestRender();
                } else{
                    _notificationService.Notify(NotificationSeverity.Error, result.Metadata?.Message ?? lang["CartError"]);
                    // Her durumda sepeti yeniden çekerek UI'ı senkron tut
                    CartResult = await _appStateManager.GetCart();
                    _shouldRender = true;
                        await RequestRender();
                    return;
                }
            } catch(Exception ex){
                try{
                    Console.WriteLine(ex);
                } catch{}
                _notificationService.Notify(NotificationSeverity.Error, lang["CartError"] ?? "İşlem sırasında bir hata oluştu.");
            }
        }, "Sepet işlemi");
    }
    
    private async Task ToggleFavorite(SellerProductViewModel product){
        
        var favoritesResult = await FavoriteService.GetAllFavoritesAsync(1, 1000);
        bool isFavorite = favoritesResult.Ok == true && favoritesResult.Result?.Data?.Any(f => f.Id == product.ProductId) == true;
        if(isFavorite){
            await FavoriteService.DeleteFavoriteForCurrentUserAsync(product.ProductId);
            _notificationService.Notify(NotificationSeverity.Info, lang["FavoriteRemoved"] ?? "Favoriden çıkarıldı");
        } else{
            var result = await FavoriteService.UpsertFavoriteForCurrentUserAsync(product.ProductId);
            if(result.Ok){
                _notificationService.Notify(NotificationSeverity.Success, lang["FavoriteAdded"] ?? "Favorilere eklendi");
            } else{
                _notificationService.Notify(NotificationSeverity.Error, lang["FavoriteAddFailed"] ?? "Favori eklenemedi");
            }
        }
        _shouldRender = true;
    }

 
    private List<SellerProductViewModel> AlternativeProducts = new();
    private bool AreAlternativesLoaded = false;
    private bool IsLoadingAlternatives = false;

    private async Task SetActiveTab(string tabName)
    {
        activeTab = tabName;
        _shouldRender = true;
        await InvokeAsync(StateHasChanged);

        if(activeTab == "installment" && !AreInstallmentsLoaded && !IsLoadingInstallments)
        {
            await LoadInstallmentOptionsAsync();
        }
        else if(activeTab == "alternatives" && !AreAlternativesLoaded && !IsLoadingAlternatives)
        {
            await LoadAlternativeProductsAsync();
        }
    }

    private async Task LoadAlternativeProductsAsync()
    {
        if (IsLoadingAlternatives) return;
        
        IsLoadingAlternatives = true;
        _shouldRender = true;
        await InvokeAsync(StateHasChanged);
        
        try
        {
            var oemCodes = GetAlternateOemCodes(); // Method already exists in file
            if(oemCodes != null && oemCodes.Count > 0)
            {
                var result = await _productService.GetByOemCodesAsync(oemCodes.ToList());
                if(result.Ok && result.Result != null)
                {
                    // Exclude current product and sort by stock
                    AlternativeProducts = result.Result
                        .Where(p => p.ProductId != Product?.ProductId)
                        .OrderByDescending(p => p.Stock > 0)
                        .ThenBy(p => p.SalePrice)
                        .ToList();
                }
            }
            AreAlternativesLoaded = true;
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Alternatif ürünler yüklenirken hata: {ex.Message}");
        }
        finally
        {
            IsLoadingAlternatives = false;
            _shouldRender = true;
                await RequestRender();
        }
    }

    private async Task LoadInstallmentOptionsAsync()
    {
        if (IsLoadingInstallments) return;
        
        IsLoadingInstallments = true;
        _shouldRender = true;
        await InvokeAsync(StateHasChanged);
        var tempList = new List<ecommerce.Web.ViewModels.BankInstallmentViewModel>();
        try
        {
            // Create a new scope to ensure we have a fresh DbContext that isn't being used by other async tasks
            using var scope = _scopeFactory.CreateScope();
            var scopedBankService = scope.ServiceProvider.GetRequiredService<IBankService>();
           
            var banksResult = await scopedBankService.GetActiveBanksAsync();
            if (banksResult.Ok && banksResult.Result != null)
            {
                

                // Sequential execution to prevent EF Core concurrency issues (DbContext is not thread-safe)
                foreach (var bank in banksResult.Result)
                {
                    var bankVm = new ecommerce.Web.ViewModels.BankInstallmentViewModel
                    {
                        BankId = bank.Id,
                        BankName = bank.Name,
                        BankLogo = bank.LogoPath
                    };

                    var cardsResult = await scopedBankService.GetBankCardsAsync(bank.Id);
                    if (cardsResult.Ok && cardsResult.Result != null)
                    {
                        foreach (var card in cardsResult.Result)
                        {
                            var cardVm = new ecommerce.Web.ViewModels.CardInstallmentViewModel
                            {
                                CardId = card.Id, // Fixed: BankCardListDto uses Id, not CardId
                                CardName = card.Name
                            };

                            // Fixed: BankCardListDto uses Id here as well
                            var instResult = await scopedBankService.GetBankInstallmentsAsync(card.Id);
                            if (instResult.Ok && instResult.Result != null)
                            {
                                cardVm.Installments = instResult.Result.OrderBy(x => x.Installment).ToList();
                            }
                            
                            // Only add card if it has installments (or strategy choice: show all?)
                            // User request implies showing active structure. 
                            // Usually we want to verify coverage. 
                            // Adding card even if empty for now, or filter later.
                            bankVm.Cards.Add(cardVm);
                        }
                    }
                    
                    if(bankVm.Cards.Any())
                    {
                        tempList.Add(bankVm);
                    }
                }

                BankInstallments = tempList;
                AreInstallmentsLoaded = true;
            }
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Taksitler yüklenirken hata: {ex.Message}");
        }
        finally
        {
            IsLoadingInstallments = false;
            _shouldRender = true;
                await RequestRender();
        }
    }

    private IReadOnlyList<string> GetProductCompatibilitySummaries()
    {
        if(Product?.PerfectCompatibilityCars == null || Product.PerfectCompatibilityCars.Count == 0){
            return Array.Empty<string>();
        }

        return Product.PerfectCompatibilityCars
            .Select(FormatCompatibilitySummary)
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .Distinct(CompatibilityComparer)
            .ToList();
    }

    private string FormatCompatibilitySummary(SellerProductCompatibilityDto car)
    {
        if(car == null){
            return string.Empty;
        }

        var segments = new List<string>();

        if(!string.IsNullOrWhiteSpace(car.PlateNumber)){
            segments.Add(car.PlateNumber.Trim().ToUpperInvariant());
        }

        var modelSegments = new List<string>();

        if(!string.IsNullOrWhiteSpace(car.ManufacturerName)){
            modelSegments.Add(car.ManufacturerName.Trim());
        }

        if(!string.IsNullOrWhiteSpace(car.BaseModelName)){
            modelSegments.Add(car.BaseModelName.Trim());
        }

        if(!string.IsNullOrWhiteSpace(car.SubModelName)){
            modelSegments.Add(car.SubModelName.Trim());
        }

        if(modelSegments.Count == 0){
            var fallback = new List<string>();
            if(!string.IsNullOrWhiteSpace(car.ManufacturerKey)){
                fallback.Add(car.ManufacturerKey.Trim().ToUpperInvariant());
            }
            if(!string.IsNullOrWhiteSpace(car.BaseModelKey)){
                fallback.Add(car.BaseModelKey.Trim().ToUpperInvariant());
            }
            if(!string.IsNullOrWhiteSpace(car.SubModelKey)){
                fallback.Add(car.SubModelKey.Trim().ToUpperInvariant());
            }
            if(fallback.Count > 0){
                modelSegments.Add(string.Join(" ", fallback));
            }
        }

        if(modelSegments.Count > 0){
            segments.Add(string.Join(" ", modelSegments));
        }

        if(segments.Count == 0){
            return lang["Product.Compatibility.UnknownCar"] ?? "Kayıtlı araç";
        }

        return string.Join(" • ", segments);
    }

    private string? GetPrimaryOemCode()
    {
        var partNumber = Product?.PartNumber;
        if(!string.IsNullOrWhiteSpace(partNumber)){
            return partNumber.Trim();
        }

        // GroupCode artık array - direkt kullan
        return Product?.OemCode?.FirstOrDefault();
    }

    private IReadOnlyList<string> GetAlternateOemCodes()
    {
        // GroupCode artık array - direkt kullan
        if(Product?.OemCode == null || !Product.OemCode.Any()){
            return Array.Empty<string>();
        }
        var groupCodes = Product.OemCode;

        var primary = GetPrimaryOemCode();
        if(string.IsNullOrWhiteSpace(primary)){
            return groupCodes;
        }

        return groupCodes
            .Where(code => !string.Equals(code, primary, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ParseGroupCodes(string? rawGroupCodes)
    {
        var result = new List<string>();
        if(string.IsNullOrWhiteSpace(rawGroupCodes)){
            return result;
        }

        var primaryTokens = rawGroupCodes
            .Split(GroupCodePrimarySeparators, StringSplitOptions.RemoveEmptyEntries);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var token in primaryTokens){
            var trimmedToken = token?.Trim();
            if(string.IsNullOrWhiteSpace(trimmedToken)){
                continue;
            }

            var secondaryTokens = trimmedToken
                .Split(GroupCodeSecondarySeparators, StringSplitOptions.RemoveEmptyEntries);

            if(secondaryTokens.Length > 1){
                foreach(var secondary in secondaryTokens){
                    AddCodeIfValid(seen, result, secondary);
                }
            } else{
                AddCodeIfValid(seen, result, trimmedToken);
            }
        }

        return result;
    }

    private static void AddCodeIfValid(HashSet<string> seen, List<string> list, string code)
    {
        if(string.IsNullOrWhiteSpace(code)){
            return;
        }

        var normalized = code.Trim();
        if(normalized.Length < 2){
            return;
        }

        if(seen.Add(normalized)){
            list.Add(normalized);
        }
    }
    
    private async Task LoadCompatibleVehiclesAsync(bool showGlobalLoading = true)
    {
        if (Product?.ProductId <= 0)
        {
            vehiclesLoaded = true;
            _shouldRender = true;
                await RequestRender();
            return;
        }

        vehiclesLoaded = false;
        _shouldRender = true;
        await InvokeAsync(StateHasChanged);

        Func<Task> loader = async () =>
        {
            try
            {
                var result = await _productService.GetCompatibleModelsAsync(Product.ProductId);
                if (result.Ok && result.Result != null)
                {
                    compatibleModels = new List<BaseModelDto>(result.Result);
                    
                    if (compatibleModels.Any())
                    {
                        selectedBaseModelKey = compatibleModels.First().BaseModelKey;
                    }
                    
                    // Eğer birden fazla base model varsa manufacturer listesi oluştur
                    var tempManufacturers = compatibleModels
                        .Where(m => !string.IsNullOrEmpty(m.ManufacturerName))
                        .Select(m => m.ManufacturerName!)
                        .Distinct()
                        .OrderBy(m => m)
                        .ToList();
                    
                    manufacturers = new List<string>(tempManufacturers);
                    selectedManufacturer = manufacturers.FirstOrDefault() ?? "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uyumlu araçlar yüklenirken hata: {ex.Message}");
            }
            finally
            {
                vehiclesLoaded = true;
                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }
        };

        if (showGlobalLoading)
        {
            await _appStateManager.ExecuteWithLoading(loader, "Uyumlu araçlar yükleniyor");
        }
        else
        {
            await loader();
        }
    }

    private void SelectManufacturer(string manufacturer)
    {
        selectedManufacturer = manufacturer;
        currentPage = 1;
        expandedModels.Clear();
        _shouldRender = true;
    }

    private List<BaseModelDto> GetFilteredModels()
    {
        if (!compatibleModels.Any())
            return new List<BaseModelDto>();

        var filtered = compatibleModels.AsEnumerable();

        // Marka filtreleme
        if (!string.IsNullOrEmpty(selectedManufacturer))
        {
            filtered = filtered.Where(m => m.ManufacturerName == selectedManufacturer);
        }

        // Arama filtreleme
        if (!string.IsNullOrEmpty(searchQuery))
        {
            var search = searchQuery.ToLower();
            filtered = filtered.Where(m => 
                (m.Name?.ToLower().Contains(search) ?? false) ||
                (m.ManufacturerName?.ToLower().Contains(search) ?? false) ||
                (m.SubModels?.Any(sm => sm.Name?.ToLower().Contains(search) ?? false) ?? false)
            );
        }

        return filtered.ToList();
    }
    
    private List<BaseModelDto> GetPaginatedModels()
    {
        var filtered = GetFilteredModels();
        return filtered.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
    }
    
    private int GetTotalPages()
    {
        var filtered = GetFilteredModels();
        return (int)Math.Ceiling((double)filtered.Count / pageSize);
    }
    
    private int GetTotalFilteredCount()
    {
        return GetFilteredModels().Count;
    }
    
    private List<int> GetPageNumbers()
    {
        var totalPages = GetTotalPages();
        var pages = new List<int>();
        
        var startPage = Math.Max(1, currentPage - 2);
        var endPage = Math.Min(totalPages, currentPage + 2);
        
        for (int i = startPage; i <= endPage; i++)
        {
            pages.Add(i);
        }
        
        return pages;
    }
    
    private void ChangePage(int page)
    {
        if (page < 1) page = 1;
        var totalPages = GetTotalPages();
        if (page > totalPages) page = totalPages;
        
        currentPage = page;
        expandedModels.Clear();
        
        Console.WriteLine($"📄 SAYFA DEĞİŞTİ: {currentPage}/{totalPages}");
        
        _shouldRender = true;
    }
    
    private void ToggleModelDetails(string modelKey)
    {
        if (expandedModels.Contains(modelKey))
        {
            expandedModels.Remove(modelKey);
        }
        else
        {
            expandedModels.Add(modelKey);
        }
        _shouldRender = true;
    }
    
    private void OnSearchChanged()
    {
        currentPage = 1;
        expandedModels.Clear();
        _shouldRender = true;
    }
    
    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _appStateManager.StateChanged -= AppState_StateChanged;
        Navigation.LocationChanged -= OnLocationChanged;
        
        try {
            _renderCts?.Cancel();
            _renderCts?.Dispose();
        } catch { }
    }
}
