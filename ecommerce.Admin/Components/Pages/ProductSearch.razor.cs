using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Radzen;
using Radzen.Blazor;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Domain.Shared.Dtos.Search;
using VinElasticDto = ecommerce.Web.Domain.Dtos.VinElasticDto;

namespace ecommerce.Admin.Components.Pages
{
    public partial class ProductSearch : ComponentBase, IDisposable
    {
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected IAdminProductSearchService ProductSearchService { get; set; } = default!;
        [Inject] protected ecommerce.Admin.Domain.Interfaces.ICategoryService CategoryService { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Parameter] [SupplyParameterFromQuery] public int? DiscountId { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; } = default!;
        [Inject] protected IConfiguration Configuration { get; set; } = default!;
        [Inject] protected Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] protected DialogService DialogService { get; set; } = default!;
        [Inject] protected TooltipService TooltipService { get; set; } = default!;
        [Inject] protected ecommerce.Web.Domain.Services.Abstract.ICartService CartService { get; set; } = default!;
        [Inject] protected ecommerce.Admin.Services.AuthenticationService Security { get; set; } = default!;
        [Inject] protected IRecentSearchService RecentSearchService { get; set; } = default!;
        [Inject] protected ecommerce.Admin.Domain.Interfaces.IDiscountService DiscountService { get; set; } = default!;
        [Inject] protected ecommerce.Admin.Services.Interfaces.IDashboardCacheService DashboardCacheService { get; set; } = default!;
        [Inject] protected ISearchSynonymService SynonymService { get; set; } = default!;
        [Inject] protected ISearchAnalyticsService SearchAnalyticsService { get; set; } = default!;
        [Inject] protected ISearchFieldMatcherService FieldMatcherService { get; set; } = default!;
        [Inject] protected ecommerce.Admin.Services.Interfaces.IVinService VinService { get; set; } = default!;
        [Inject] protected ILogger<ProductSearch> Logger { get; set; } = default!;

        protected string searchText = "";
        protected bool isLoading = false;
        protected bool isLoadingVin = false; // VIN decode için özel loading flag
        protected bool showOnlyInStock = false;
        protected bool showOnlyWithImage = false;
        protected string activeTab = "products";
        protected bool isTableView = true;
        protected bool hasSearched = false;
        protected bool isGroupedBySeller = true; // Default olarak aktif
        protected int page = 0;
        
        // Recent Searches
        protected List<RecentSearchDto> recentSearches = new();
        protected bool isSearchFocused = false;
        protected bool isRecentSearchLoading = false;
        protected bool isRecentSearchesDropdownOpen = false;
        protected string? hoveredSearch = null;
        protected string? loadingRecentSearch = null;

        // Cart functionality

        // Filter states
        protected double? minPrice = null;
        protected double? maxPrice = null;
        protected List<int> selectedCategoryIds = new();
        protected List<int> selectedBrandIds = new();
        protected List<int> selectedProductIds = new();
        protected HashSet<string> selectedDotPartNames = new();
        protected IEnumerable<string> selectedDotPartNamesList 
        { 
            get => selectedDotPartNames.ToList(); 
            set => selectedDotPartNames = value?.ToHashSet() ?? new HashSet<string>(); 
        }
        protected HashSet<string> selectedManufacturerNames = new();
        protected HashSet<string> selectedBaseModelNames = new();
        protected HashSet<string> selectedSubModelNames = new();
        protected List<string> selectedDatProcessNumbers = new();
        protected List<string> selectedOemCodes = new();
        
        // Vehicle filter states (keep separate from search text)
        protected string? selectedManufacturerName = null;
        protected string? selectedManufacturerKey = null;
        protected string? selectedBaseModelName = null;
        protected string? selectedBaseModelKey = null;
        protected string? selectedSubModelName = null;
        protected string? selectedSubModelKey = null;
        
        // Vehicle Match Modal states
        protected bool isVehicleMatchModalOpen = false;
        protected string? modalSelectedManufacturer = null;
        protected string? modalSelectedModel = null;
        protected string? modalSelectedSubModel = null;
        protected string? modalSelectedDotPart = null;
        protected string? modalSelectedDotPartDisplay = null;
        protected List<string>? modalSelectedDotPartProcessNumber = null;
        protected List<string> modalBaseModels = new();
        protected List<string> modalManufacturers = new(); // NEW: Separate list for modal manufacturers
        protected List<SubModelDto> modalSubModels = new();
        public class ModalDotPartDto 
        { 
            public string Name { get; set; } = null!; 
            public string DisplayName { get; set; } = null!;
            public List<string>? ProcessNumber { get; set; } 
        }

        protected List<ModalDotPartDto> modalDotParts = new();
        
        // Cache for model->submodels mapping
        // Cache for model->submodels mapping
        // Cache for model->submodels mapping
        protected Dictionary<string, List<SubModelDto>> modalModelSubModelsCache = new();
        
        // [VIN-FIX] Persist ManufacturerKeys from VIN search to apply in grid filters
        protected List<string> vinManufacturerKeys = new();
        
        // [VIN-FIX] Track currently selected VIN vehicle for fallback
        protected ecommerce.Admin.Services.Dtos.VinDto.VinVehicleDto? currentVinVehicle;

        // Columnar Modal Filter Properties
        protected string modalManufacturerSearch = "";
        protected string modalModelSearch = "";

        protected string modalSubModelSearch = "";
        protected string modalDotPartSearch = "";

        protected List<string> filteredModalManufacturers => modalManufacturers
            .Where(m => string.IsNullOrEmpty(modalManufacturerSearch) || m.Contains(modalManufacturerSearch, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToList();

        protected List<string> filteredModalModels => modalBaseModels
            .Where(m => string.IsNullOrEmpty(modalModelSearch) || m.Contains(modalModelSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();

        protected List<SubModelDto> filteredModalSubModels => modalSubModels
            .Where(m => !string.IsNullOrWhiteSpace(m.Name) && 
                       (string.IsNullOrEmpty(modalSubModelSearch) || 
                       (m.Name.Contains(modalSubModelSearch, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        protected List<ModalDotPartDto> filteredModalDotParts => modalDotParts
            .Where(d => string.IsNullOrEmpty(modalDotPartSearch) || 
                       d.Name.Contains(modalDotPartSearch, StringComparison.OrdinalIgnoreCase) || 
                       d.DisplayName.Contains(modalDotPartSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
        // UI states for collapsible sections
        protected bool isCategoriesExpanded = false;
        protected bool isBrandsExpanded = false;
        protected bool isPriceExpanded = false;
        protected bool isPartsExpanded = false;
        protected bool isVehiclesExpanded = true; // Open by default

        // Image Zoom states
        protected string? zoomedImageUrl = null;
        protected bool isImageZoomed = false;

        // Vehicle Modal states
        // Vehicle Modal states
        protected bool isVehicleModalOpen = false;
        protected SellerProductViewModel? selectedProductForVehicles = null;

        // Loading states for modal columns
        protected bool isModelLoading = false;
        protected bool isSubModelLoading = false;
        protected bool isDotPartLoading = false;
        protected bool isCalculatingVinCounts = false;

        protected Paging<List<SellerProductViewModel>> searchResult = new() { Data = new List<SellerProductViewModel>() };
        protected List<BrandDto> brands = new();
        
        // Discounts/Campaigns
        protected List<ecommerce.Admin.Domain.Dtos.DiscountDto.DiscountWithProductsDto> activeDiscounts = new();
        protected bool isLoadingDiscounts = false;
        protected bool isDiscountDetailModalOpen = false;
        protected ecommerce.Admin.Domain.Dtos.DiscountDto.DiscountWithProductsDto? selectedDiscount = null;
        
        // UI Helpers
        protected bool priceDropdownOpen = false;
        protected bool isFilterVisible = false; 
        
        // Price Slider
        protected IEnumerable<double> priceRange = new double[] { 0, 100000 };
        protected double priceMaxLimit = 100000;

        protected async Task OnPriceChange(IEnumerable<double> value)
        {
            if (value != null && value.Count() == 2)
            {
                minPrice = value.ElementAt(0);
                maxPrice = value.ElementAt(1);
                await SearchProducts(preserveFilterOptions: true);
            }
        } 

        protected bool isModalOnlyInStock = false;

        protected void ToggleFilters()
        {
            isFilterVisible = !isFilterVisible;
        }

        protected async Task OnSearchFiltersChanged()
        {
            page = 1; // [FILTER-FIX] Reset page to avoid empty results
            await SearchProducts(preserveFilterOptions: true);
        }

        protected async Task OnManufacturerChanged(string name)
        {
            selectedManufacturerName = name;
            selectedBaseModelName = null;
            selectedSubModelName = null;
            await SearchProducts(preserveFilterOptions: false);
        }

        protected async Task OnModelChanged(string name)
        {
            selectedBaseModelName = name;
            selectedSubModelName = null;
            await SearchProducts(preserveFilterOptions: false);
        }

        protected async Task OnSubModelChanged(string name)
        {
            selectedSubModelName = name;
            await SearchProducts(preserveFilterOptions: false);
        }
        protected List<CategoryDto> categories = new();
        protected List<string> dotPartNames = new();
        protected List<string> manufacturerNames = new();
        protected List<string> baseModelNames = new();
        protected List<string> subModelNames = new();
        protected List<CompatibleVehicleDto> compatibleVehicles = new();

        // Cart loading state (separate from page loading)
        protected bool isCartLoading = false;
        private bool _cartLoadedOnce = false;
        
        protected string SelectedCustomerName { get; set; } = string.Empty;
        
        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Removed global isLoading to prevent search view from overriding dashboard on load
                
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                
                if (Security != null)
                {
                    await Security.InitializeAsync(authState);
                }

                await base.OnInitializedAsync();
                isCartVisible = IsCartRoleAllowed();

                // Subscribe to cart changes
                if (CartStateService != null)
                {
                    CartStateService.OnChange += OnCartStateChanged;
                }

                // Subscribe to customer changes
                if (Security != null)
                {
                    Security.OnSelectedCustomerChanged += OnSelectedCustomerChanged;
                }

                // Load initial data with wait-for-identity mechanism
                await LoadInitialDataWithRetry();
                
                // OPTIMIZATION: Load recent searches immediately
                await LoadRecentSearches();

                // Check if discountId parameter exists and apply discount filter
                if (DiscountId.HasValue)
                {
                    await ApplyDiscountById(DiscountId.Value);
                }
            }
            catch(Exception ex)
            {
                // Log error silently
            }
        }

        private async Task LoadInitialDataWithRetry()
        {
            isLoadingDashboard = true;
            StateHasChanged();

            // Wait for User Identity to be fully initialized in the circuit
            int retryCount = 0;
            while ((Security.User == null || Security.User.Id == 0) && retryCount < 15)
            {
                await Task.Delay(200);
                retryCount++;
            }

            // Load dashboard data once identity is ready
            await LoadDashboardData();
            
            // Also refresh cart once identity is ready
            if (CartStateService != null)
            {
                await CartStateService.RefreshCart();
            }
        }
        
        protected override async Task OnParametersSetAsync()
        {
            // Handle discountId parameter when navigating
            if (DiscountId.HasValue)
            {
                await ApplyDiscountById(DiscountId.Value);
            }
            await base.OnParametersSetAsync();
        }
        
        protected async Task ApplyDiscountById(int discountId)
        {
            try
            {
                // Load active discounts to find the one with matching ID
                var result = await DiscountService.GetActiveDiscountsWithProductsAsync();
                if (result.Ok && result.Result != null)
                {
                    var discount = result.Result.FirstOrDefault(d => d.Id == discountId);
                    if (discount != null)
                    {
                        await ApplyDiscountSearch(discount);
                    }
                    else
                    {
                        // Fallback: If not found in active list (maybe date/type filter in service), try direct load
                        var directResult = await DiscountService.GetDiscountById(discountId);
                        if (directResult.Ok && directResult.Result != null)
                        {
                            var manualDiscount = new ecommerce.Admin.Domain.Dtos.DiscountDto.DiscountWithProductsDto
                            {
                                Id = directResult.Result.Id ?? 0,
                                Name = directResult.Result.Name,
                                DiscountType = directResult.Result.DiscountType ?? DiscountType.AssignedToProducts,
                                AssignedEntityIds = directResult.Result.AssignedEntityIds
                            };
                            await ApplyDiscountSearch(manualDiscount);
                        }
                    }
                }
            }
            catch
            {
                // Silent fail
            }
        }

        protected async Task LoadDashboardData()
        {
            try
            {
                // Get user ID for cache key
                var userId = Security?.User?.Id ?? 0;
                if (userId == 0)
                {
                    isLoadingDashboard = false;
                    return;
                }
                
                // B2B Impersonation: Use SelectedCustomerId if available, otherwise User.CustomerId
                var customerId = Security.SelectedCustomerId ?? Security?.User?.CustomerId;
                
                // Fetch dashboard data (cache service handles loading/retry internally)
                var dashboardData = await DashboardCacheService.GetDashboardDataAsync(userId, customerId);
                
                if (dashboardData != null)
                {
                    pendingOrderCount = dashboardData.PendingOrderCount;
                    pendingOrderTotal = dashboardData.PendingOrderTotal;
                    totalOrderCount = dashboardData.TotalOrderCount;
                    totalOrderAmount = dashboardData.TotalOrderAmount;
                    balance = dashboardData.Balance;
                    totalInvoiceCount = dashboardData.TotalInvoiceCount;
                    totalInvoiceAmount = dashboardData.TotalInvoiceAmount;
                    totalDebit = dashboardData.TotalDebit;
                    totalCredit = dashboardData.TotalCredit;
                    linkedCustomerCount = dashboardData.LinkedCustomerCount;
                }
            }
            catch (Exception ex)
            {
                // Silent fail - use default values
            }
            finally
            {
                isLoadingDashboard = false;
                StateHasChanged();
            }
        }
        
        private int totalInvoiceCount = 0;
        private decimal totalInvoiceAmount = 0;
        private decimal totalDebit = 0;
        private decimal totalCredit = 0;
        private int linkedCustomerCount = 0;
        


        private async void OnSelectedCustomerChanged()
        {
            try
            {
                // When customer context changes, reload dashboard and cart
                await InvokeAsync(async () => {
                    isLoadingDashboard = true;
                    StateHasChanged();
                    
                    // Refresh cart for the new customer context
                    if (CartStateService != null)
                        await CartStateService.RefreshCart();
                    
                    // Reload dashboard metrics
                    await LoadDashboardData();
                    await LoadPurchasedProductIdsAsync();
                    
                    await InvokeAsync(StateHasChanged);
                });
            }
            catch
            {
                // Silent fail
            }
        }

        protected async Task LoadPurchasedProductIdsAsync()
        {
            var customerId = Security?.SelectedCustomerId ?? Security?.User?.CustomerId ?? 0;
            if (customerId <= 0) { purchasedProductIds.Clear(); return; }
            var productIds = searchResult?.Data?.Select(p => p.ProductId).ToList();
            if (productIds == null || !productIds.Any()) { purchasedProductIds.Clear(); return; }
            try
            {
                var ids = await OrderService.GetPurchasedProductIdsByCustomer(customerId, productIds);
                purchasedProductIds = ids?.ToHashSet() ?? new HashSet<int>();
            }
            catch
            {
                purchasedProductIds.Clear();
            }
        }

        protected async Task OpenPurchaseHistoryModal(SellerProductViewModel product)
        {
            var customerId = Security?.SelectedCustomerId ?? Security?.User?.CustomerId ?? 0;
            if (customerId <= 0)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Geçmiş Alışveriş", "Geçmiş alışverişleri görmek için lütfen bir cari seçin.");
                return;
            }
            await DialogService.OpenAsync<Modals.PurchaseHistoryModal>(
                "Geçmiş Alışverişler",
                new Dictionary<string, object>
                {
                    ["ProductId"] = product.ProductId,
                    ["ProductName"] = product.ProductName ?? "",
                    ["CustomerId"] = customerId
                },
                new DialogOptions { Width = "700px", Height = "450px", Resizable = true });
        }

        protected string GetCartTotal()
        {
            var cart = CartStateService?.CurrentCart;
            var total = cart?.OrderTotal ?? 0m;
            return total.ToString("C");
        }

        protected int GetCartItemCount()
        {
            var cart = CartStateService?.CurrentCart;
            return cart?.CartCount ?? 0;
        }

        protected int GetPendingOrderCount()
        {
            return pendingOrderCount;
        }

        protected async Task NavigateToCart()
        {
            // Open CartDrawer by clicking the cart button in header
            // The cart button is in MainLayout, we'll trigger it via JS
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", @"
                    (function() {
                        var cartButton = document.querySelector('button[title=""Sepetim""]');
                        if (cartButton) {
                            cartButton.click();
                        } else {
                            // Fallback: try to find by class or other selector
                            var altButton = document.querySelector('.header-cart-dropdown') || 
                                          document.querySelector('[aria-label*=""Sepet""]') ||
                                          document.querySelector('button:has(i.fa-cart-shopping)');
                            if (altButton) altButton.click();
                        }
                    })();
                ");
            }
            catch
            {
                // Silent fail - cart button might not be available
            }
        }

        protected async Task NavigateToPendingOrders()
        {
            // Navigate to orders page with OrderNew status filter (status = 1)
            Navigation.NavigateTo("/b2b/my-orders");
        }

        protected string GetPendingOrderTotal()
        {
            return pendingOrderTotal.ToString("N2");
        }

        protected int GetTotalOrderCount()
        {
            return totalOrderCount;
        }

        protected string GetTotalOrderAmount()
        {
            return totalOrderAmount.ToString("N2");
        }

        protected decimal GetBalance()
        {
            return balance;
        }

        protected decimal GetTotalDebit() => totalDebit;
        protected decimal GetTotalCredit() => totalCredit;

        protected int GetTotalInvoiceCount()
        {
            return totalInvoiceCount;
        }

        protected string GetTotalInvoiceAmount()
        {
            return totalInvoiceAmount.ToString("C");
        }

        protected async Task LoadActiveDiscounts()
        {
            try
            {
                isLoadingDiscounts = true;
                var result = await DiscountService.GetActiveDiscountsWithProductsAsync();
                
                if (result.Ok && result.Result != null)
                {
                    activeDiscounts = result.Result;
                }
            }
            catch (Exception ex)
            {
                // Log error silently - discounts are not critical
            }
            finally
            {
                isLoadingDiscounts = false;
            }
        }
        
        /// <summary>
        /// Async cart loading - called after page renders
        /// </summary>
        private async Task LoadCartItemsAsync()
        {
            try
            {
                isCartLoading = true;
                StateHasChanged();
                
                await LoadCartItems();
            }
            catch (Exception ex)
            {
                // Log error silently
            }
            finally
            {
                isCartLoading = false;
                StateHasChanged();
            }
        }

        protected async Task LoadRecentSearches()
        {
            try
            {
                var allSearches = await RecentSearchService.GetRecentSearchesAsync();
                
                // Group by Term (case-insensitive) and take the most recent one for each term
                recentSearches = allSearches
                    .GroupBy(s => s.Term.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(s => s.SearchDate).First())
                    .OrderByDescending(s => s.SearchDate)
                    .ToList();
            }
            catch(Exception ex)
            {
                // Log error silently
            }
        }

        protected void OnSearchFocus() => isSearchFocused = true;
        
        protected async Task OnSearchBlur() 
        {
            // Small delay to allow click on recent item to register
            await Task.Delay(200);
            isSearchFocused = false;
        }

        protected async Task SelectRecentSearch(string term)
        {
            try
            {
                isRecentSearchLoading = true;
                loadingRecentSearch = term;
                isRecentSearchesDropdownOpen = false;
                StateHasChanged();
                
                searchText = term;
                await OnSearchSubmit();
            }
            finally
            {
                isRecentSearchLoading = false;
                loadingRecentSearch = null;
                StateHasChanged();
            }
        }

        protected async Task ToggleRecentSearchesDropdown()
        {
            try
            {
                if (!isRecentSearchesDropdownOpen)
                {
                    await LoadRecentSearches();
                }
                isRecentSearchesDropdownOpen = !isRecentSearchesDropdownOpen;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                // Log error silently
            }
        }

        protected void OnSearchInputFocus()
        {
            isSearchFocused = true;
        }

        protected async Task OnSearchInputBlur()
        {
            // Dropdown açıkken blur olmasını engellemek için kısa bir gecikme
            await Task.Delay(200);
            if (!isRecentSearchesDropdownOpen)
            {
                isSearchFocused = false;
            }
        }

        protected async Task CloseRecentSearchesDropdown()
        {
            await Task.Delay(100); // Hover delay
            if (isRecentSearchesDropdownOpen)
            {
                isRecentSearchesDropdownOpen = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task CloseRecentSearchesOnOutsideClick()
        {
            if (isRecentSearchesDropdownOpen)
            {
                isRecentSearchesDropdownOpen = false;
                await InvokeAsync(StateHasChanged);
            }
        }


        protected async Task ClearRecentSearches()
        {
            try
            {
                if (RecentSearchService != null)
                {
                    await RecentSearchService.ClearRecentSearchesAsync();
                    await LoadRecentSearches();
                    isRecentSearchesDropdownOpen = false;
                    await InvokeAsync(StateHasChanged);
                    NotificationService?.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Geçmiş aramalar temizlendi.",
                        Duration = 2000
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error silently
                NotificationService?.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Geçmiş aramalar temizlenirken bir hata oluştu.",
                    Duration = 3000
                });
            }
        }

        protected async Task RemoveRecentSearch(string term)
        {
            try
            {
                if (RecentSearchService != null)
                {
                    await RecentSearchService.RemoveSearchTermAsync(term);
                    await LoadRecentSearches();
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                // Log error silently
                NotificationService?.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Arama geçmişi silinirken bir hata oluştu.",
                    Duration = 3000
                });
            }
        }

        protected RadzenDataGrid<SellerProductViewModel> searchGrid = null!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Grid grouping - apply based on isGroupedBySeller state
            if (searchGrid != null && firstRender)
            {
                await UpdateGridGroups();
            }
            
            // Load cart AFTER first render - page shows immediately
            if (firstRender && isCartVisible && !_cartLoadedOnce)
            {
                _cartLoadedOnce = true;
                await LoadCartItemsAsync();
            }
            
            await base.OnAfterRenderAsync(firstRender);
        }

        private System.Threading.CancellationTokenSource? _searchCts;

        protected async Task OnSearchInput(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            searchText = text;

            // Cancel previous search task
            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;

            if (searchText.Length >= 3)
            {
                try
                {
                    // Debounce: Wait 600ms before searching
                    await Task.Delay(600, token);
                    
                    if (token.IsCancellationRequested) return;

                    // Reset vehicle context to ensure global search
                    selectedManufacturerName = null;
                    selectedBaseModelName = null;
                    selectedSubModelName = null;
                    selectedSubModelKey = null;
                    selectedDotPartNames.Clear();
                    selectedDatProcessNumbers.Clear();
                    
                    await SearchProducts();
                }
                catch (TaskCanceledException)
                {
                    // Ignore cancellation
                }
            }
            else if (string.IsNullOrEmpty(searchText))
            {
                // Clear immediately if empty
                 searchResult.Data.Clear();
                 brands.Clear();
                 compatibleVehicles.Clear();
                 StateHasChanged();
            }
        }

        protected async Task OnSearchSubmit()
        {
            if (searchText.Length >= 2)
            {
                Logger.LogInformation("[VIN-DEBUG] OnSearchSubmit başladı - searchText: '{SearchText}', Length: {Length}", 
                    searchText, searchText.Length);
                
                // Force loading state immediately
                isLoading = true;
                await InvokeAsync(StateHasChanged);

                // VIN Numarası Kontrolü (17 haneli VE sadece harf/rakam)
                var trimmedSearch = searchText.Trim().ToUpperInvariant();
                Logger.LogInformation("[VIN-DEBUG] Trimmed search: '{TrimmedSearch}', Length: {Length}", 
                    trimmedSearch, trimmedSearch.Length);
                
                // VIN kontrolü: 17 karakter, sadece harf/rakam (I, O, Q dahil edilebilir - fix için), tire/boşluk YOK
                // Fix: Kullanıcılar I, O, Q karakterlerini yanlışlıkla girebilir (1, 0 yerine). Regex'i esnetiyoruz.
                var isVinMatch = trimmedSearch.Length >= 15 && trimmedSearch.Length <= 17 && 
                                System.Text.RegularExpressions.Regex.IsMatch(trimmedSearch, @"^[A-Z0-9]{15,17}$") &&
                                !trimmedSearch.Contains("-") && // Tire varsa OEM kodudur
                                !trimmedSearch.Contains(" "); // Boşluk varsa OEM kodudur
                
                Logger.LogInformation("[VIN-DEBUG] VIN regex match: {IsMatch}", isVinMatch);
                
                if (isVinMatch)
                {
                    // Auto-correct common typos: I->1, O->0, Q->0
                    var correctedVin = trimmedSearch
                        .Replace('I', '1')
                        .Replace('O', '0')
                        .Replace('Q', '0');
                        
                    if (correctedVin != trimmedSearch)
                    {
                        Logger.LogInformation("[VIN-DEBUG] VIN düzeltildi: {Old} -> {New}", trimmedSearch, correctedVin);
                        trimmedSearch = correctedVin;
                        searchText = correctedVin; // UI'ı da güncelle
                    }

                    Logger.LogInformation("[VIN-DEBUG] VIN numarası tespit edildi - HandleVinSearch çağrılıyor");
                    // VIN numarası tespit edildi - VIN decode işlemi yap
                    await HandleVinSearch(trimmedSearch);
                    return;
                }

                Logger.LogInformation("[VIN-DEBUG] Normal arama yapılıyor (VIN değil)");
                
                // Reset vehicle context
                selectedManufacturerName = null;
                selectedBaseModelName = null;
                selectedSubModelName = null;
                selectedSubModelKey = null;
                selectedDotPartNames.Clear();
                selectedDatProcessNumbers.Clear();

                await SearchProducts();
                
                // Save Recent Search with record count (after search completes)
                var recordCount = searchResult?.DataCount ?? 0;
                _ = RecentSearchService.AddSearchTermAsync(searchText, recordCount).ContinueWith(async _ => 
                {
                    await LoadRecentSearches();
                    await InvokeAsync(StateHasChanged);
                });
            }
        }

        // VIN Araç Seçim Modal State
        protected bool isVinVehicleSelectionModalOpen = false;
        protected List<ecommerce.Admin.Services.Dtos.VinDto.VinVehicleDto> vinMatchedVehicles = new();
        protected Dictionary<string, int> vinVehicleProductCounts = new(); // Her araç için bulunan ürün sayısı (Key: ManufacturerKey_BaseModelKey)
        protected string currentVinNumber = "";
        protected string vinVehicleSearchText = ""; // Araç arama metni
        


        // Filtrelenmiş araç listesi
        protected List<ecommerce.Admin.Services.Dtos.VinDto.VinVehicleDto> FilteredVinVehicles
        {
            get
            {
                var query = vinMatchedVehicles.AsEnumerable();

                // 1. "Referansları olmayan araçlar gelmesin" - Filter out vehicles with absolutely no data
                // [VIN-FIX] Allow FALLBACK_BRAND results even if they have no parts, to provide brand/year context.
                query = query.Where(v => 
                    (v.DatProcessNumbers != null && v.DatProcessNumbers.Any()) || 
                    (v.OemParts != null && v.OemParts.Any()) ||
                    v.MatchMethod == "FALLBACK_BRAND" ||
                    v.MatchMethod == "UNIVERSAL_V11"
                );

                // 2. "Stokta olanları göster" filter
                if (showOnlyInStock)
                {
                    query = query.Where(v => {
                        // [VIN-FIX] Fallback models should stay visible even with stock filter
                        if (v.MatchMethod == "FALLBACK_BRAND") return true;
                        
                        var vKey = $"{v.ManufacturerKey}_{v.BaseModelKey}_{v.Wmi}_{v.VdsCode}_{v.ModelYear}";
                        return vinVehicleProductCounts.TryGetValue(vKey, out var count) && count > 0;
                    });
                }

                // 3. Text Search
                if (!string.IsNullOrWhiteSpace(vinVehicleSearchText))
                {
                    var searchLower = vinVehicleSearchText.ToLowerInvariant();
                    query = query.Where(v => 
                        (v.ManufacturerName?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (v.BaseModelName?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (v.Wmi?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (v.ModelYear.ToString().Contains(searchLower))
                    );
                }
                
                return query.ToList();
            }
        }

        protected async Task OnModalStockToggle(bool value)
        {
            showOnlyInStock = value;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// VIN numarası ile arama işlemini gerçekleştirir
        /// HER ZAMAN MODAL AÇILIR - Kullanıcı aracı seçer, sonra OEM kodları ile arama yapılır
        /// </summary>
        protected async Task HandleVinSearch(string vinNumber)
        {
            try
            {
                Logger.LogInformation("[VIN] HandleVinSearch başladı - VIN: {VIN}", vinNumber);
                
                // VIN decode için özel loading flag
                isLoadingVin = true;
                isLoading = true;
                await InvokeAsync(StateHasChanged);
                
                // VIN decode et
                var decodeResult = await VinService.DecodeVinAsync(vinNumber);

                Logger.LogInformation("[VIN] DecodeVinAsync tamamlandı - Ok: {Ok}, IsSuccess: {IsSuccess}", 
                    decodeResult.Ok, decodeResult.Result?.IsSuccess ?? false);

                if (!decodeResult.Ok || decodeResult.Result == null || !decodeResult.Result.IsSuccess)
                {
                    Logger.LogWarning("[VIN] Decode başarısız - ErrorMessage: {Error}", 
                        decodeResult.Result?.ErrorMessage ?? "null");
                    
                    NotificationService.Notify(
                        NotificationSeverity.Warning, 
                        "VIN Decode Hatası", 
                        decodeResult.Result?.ErrorMessage ?? "VIN numarası decode edilemedi"
                    );
                    isLoadingVin = false;
                    isLoading = false;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                var vinData = decodeResult.Result;

                // VIN'den eşleşen araçlar bulunamadı
                if (vinData.MatchedVehicles == null || !vinData.MatchedVehicles.Any())
                {
                    Logger.LogWarning("[VIN] Eşleşen araç bulunamadı - VIN: {VIN}", vinNumber);
                    
                    NotificationService.Notify(
                        NotificationSeverity.Warning, 
                        "Araç Bulunamadı", 
                        $"VIN: {vinNumber} - Bu VIN numarası için araç bilgisi bulunamadı."
                    );

                    isLoadingVin = false;
                    isLoading = false;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                Logger.LogInformation("[VIN] Eşleşen araç sayısı: {Count}", vinData.MatchedVehicles.Count);

                // Modal açılacak - ürün sayısı hesaplama KALDIRILDI (performans için)
                vinMatchedVehicles = vinData.MatchedVehicles;
                currentVinNumber = vinNumber;
                vinManufacturerKeys.Clear(); // [VIN-FIX] Clear previous keys
                
                isVinVehicleSelectionModalOpen = true;
                isLoadingVin = false;
                isLoading = false;
                
                Logger.LogInformation("[VIN] Modal açılıyor - isVinVehicleSelectionModalOpen: {IsOpen}", isVinVehicleSelectionModalOpen);
                
                await InvokeAsync(StateHasChanged);

                // PERFORMANS: Ürün sayısı hesaplama ARKA PLANDA çalışsın (modal açıldıktan sonra)
                // Fix: Race Condition önlemek için listenin kopyasını gönderiyoruz
                var vehiclesForCount = vinMatchedVehicles.ToList();
                _ = Task.Run(async () => {
                    await CalculateProductCountsForVehicles(vehiclesForCount);
                    await InvokeAsync(StateHasChanged);
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VIN] HandleVinSearch hatası - VIN: {VIN}", vinNumber);
                NotificationService.Notify(
                    NotificationSeverity.Error, 
                    "VIN Arama Hatası", 
                    $"VIN arama sırasında hata oluştu: {ex.Message}"
                );
                isLoadingVin = false;
                isLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Seçilen VIN aracı için OEM kodları ile ürün araması yapar
        /// </summary>
        protected async Task ProcessSelectedVinVehicle(ecommerce.Admin.Services.Dtos.VinDto.VinVehicleDto vehicle, string vinNumber)
        {
            try
            {
                // [VIN-FIX] Ensure manufacturer keys are set from the SELECTED vehicle before processing
                currentVinVehicle = vehicle; // Keep track of selected vehicle for restoration
                if (!string.IsNullOrEmpty(vehicle.ManufacturerKey))
                {
                    vinManufacturerKeys = new List<string> { vehicle.ManufacturerKey };
                }
                else
                {
                    vinManufacturerKeys.Clear();
                }
                
                // MODAL KAPAT - UI güncellemesi için
                isVinVehicleSelectionModalOpen = false;
                await InvokeAsync(StateHasChanged);

                Logger.LogInformation("[VIN] Araç seçildi - {Manufacturer} {Model}, DatProcessNumbers: {DpnCount}, OemParts: {OemPartsCount}", 
                    vehicle.ManufacturerName, vehicle.BaseModelName, vehicle.DatProcessNumbers?.Count ?? 0, vehicle.OemParts?.Count ?? 0);

                // DatProcessNumbers'ı topla (PRIMARY - Öncelikli)
                var datProcessNumbers = (vehicle.DatProcessNumbers ?? new List<string>())
                    .Where(dpn => !string.IsNullOrWhiteSpace(dpn))
                    .Select(dpn => dpn.Trim())
                    .Distinct()
                    .ToList();

                // OEM Kodlarını topla (FALLBACK - Yedek)
                var vehicleOemCodes = new List<string>();
                if (vehicle.OemParts != null)
                {
                    foreach (var part in vehicle.OemParts)
                    {
                        if (string.IsNullOrWhiteSpace(part.Oem)) continue;
                        
                        var cleaned = part.Oem.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
                        if (!string.IsNullOrEmpty(cleaned))
                        {
                            vehicleOemCodes.Add(cleaned);

                            // Variation 1: Remove leading zeros (e.g., 00000123 -> 123)
                            var noZeros = cleaned.TrimStart('0');
                            if (!string.IsNullOrEmpty(noZeros) && noZeros != cleaned)
                            {
                                vehicleOemCodes.Add(noZeros);
                            }
                        }
                    }
                    vehicleOemCodes = vehicleOemCodes.Distinct().ToList();
                }

                    // [VIN-FIX] DatProcessNumbers ve OEM kodları bulunamadıysa uyar ama ARAÇ BİLGİSİNİ GÖSTER
                if (!datProcessNumbers.Any() && !vehicleOemCodes.Any())
                {
                    Logger.LogWarning("[VIN] DatProcessNumbers ve OEM kodları bulunamadı - Araç: {Manufacturer} {Model}", 
                        vehicle.ManufacturerName, vehicle.BaseModelName);
                    
                    // Araç filtrelerini ayarla
                    selectedManufacturerName = vehicle.ManufacturerName;
                    selectedBaseModelName = vehicle.BaseModelName;
                    selectedSubModelName = null;
                    selectedSubModelKey = null;

                    // Input'u temizle
                    searchText = "";
                    
                    // [VIN-FIX] Force a search with empty result but valid vehicle context
                    // This triggers the "Vehicle Found" card in the UI
                    searchResult = new Paging<List<SellerProductViewModel>> { Data = new List<SellerProductViewModel>(), DataCount = 0 };
                    hasSearched = true;
                    isLoading = false;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                // Hangi arama yöntemi kullanılacak?
                string searchMethod = datProcessNumbers.Any() ? "DatProcessNumber" : "OEM Kodları";
                int codeCount = datProcessNumbers.Any() ? datProcessNumbers.Count : vehicleOemCodes.Count;
                
                Logger.LogInformation("[VIN] {Method} ile arama yapılacak - {Count} kod bulundu", 
                    searchMethod, codeCount);

                // Araç filtrelerini ayarla ve diğer çakışabilecek filtreleri temizle
        selectedCategoryIds.Clear();
        selectedBrandIds.Clear();
        selectedProductIds.Clear();
        selectedDotPartNames.Clear();
        minPrice = null;
        maxPrice = null;

        selectedManufacturerName = vehicle.ManufacturerName;
        selectedManufacturerKey = vehicle.ManufacturerKey;
        selectedBaseModelName = vehicle.BaseModelName;
        selectedBaseModelKey = vehicle.BaseModelKey;
                selectedSubModelName = null;
                selectedSubModelKey = null;

                // searchText'i temizle - kodlar filter içinde kullanılacak
                searchText = "";
                
                // DatProcessNumbers ile arama yap (PRIMARY)
                bool dpnSearchPerformed = false;
                if (datProcessNumbers.Any())
                {
                    // Normalize to Uppercase for consistent searching
                    var normalizedDpns = datProcessNumbers.Select(x => x.ToUpperInvariant()).ToList();
                    await SearchProductsByDatProcessNumbers(normalizedDpns);
                    dpnSearchPerformed = true;
                }
                
                // Fallback to OEM Codes if DPN search yielded no results OR was not performed
                if ((!dpnSearchPerformed || searchResult?.DataCount == 0) && vehicleOemCodes.Any())
                {
                    if (dpnSearchPerformed)
                    {
                        Logger.LogWarning("[VIN] DPN araması sonuçsuz kaldı, OEM kodları ile deneniyor. DPN Count: {DpnCount}, OEM Count: {OemCount}", 
                            datProcessNumbers.Count, vehicleOemCodes.Count);
                    }
                    
                    await SearchProductsByOemCodes(vehicleOemCodes);
                }
                else if (dpnSearchPerformed && searchResult?.DataCount == 0 && !vehicleOemCodes.Any())
                {
                     // DPN arandı, sonuç yok VE OEM kodu da yok -> Sonuç yok
                }

                // Recent search kaydet - Sadece VIN numarası (prefix yok)
                var vinRecordCount = searchResult?.DataCount ?? 0;
                _ = RecentSearchService.AddSearchTermAsync(vinNumber, vinRecordCount).ContinueWith(async _ => 
                {
                    await LoadRecentSearches();
                    await InvokeAsync(StateHasChanged);
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VIN] ProcessSelectedVinVehicle hatası");
                NotificationService.Notify(
                    NotificationSeverity.Error, 
                    "Araç İşleme Hatası", 
                    $"Araç işlenirken hata oluştu: {ex.Message}"
                );
                
                // Hata durumunda loading'i kapat
                isLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// VIN araç seçim modalından araç seçildiğinde çağrılır
        /// </summary>
        protected async Task OnVinVehicleSelected(ecommerce.Admin.Services.Dtos.VinDto.VinVehicleDto vehicle)
        {
            isVinVehicleSelectionModalOpen = false;
            await InvokeAsync(StateHasChanged);
            await ProcessSelectedVinVehicle(vehicle, currentVinNumber);
        }

        /// <summary>
        /// Her araç için Elasticsearch'te bulunan ürün sayısını hesaplar
        /// PERFORMANS: Tüm araçların kodlarını tek sorguda toplar
        /// </summary>
        protected async Task CalculateProductCountsForVehicles(List<ecommerce.Admin.Services.Dtos.VinDto.VinVehicleDto> vehicles)
        {
            try
            {
                isCalculatingVinCounts = true;
                vinVehicleProductCounts.Clear();
                
                Logger.LogInformation("[VIN-COUNT] Ürün sayısı hesaplanıyor - Araç sayısı: {Count}", vehicles.Count);

                if (vehicles == null || !vehicles.Any())
                {
                     Logger.LogWarning("[VIN-COUNT] Araç listesi BOŞ! Hesaplama yapılamıyor.");
                     return;
                }
                
                // Tüm araçların DatProcessNumbers'ını topla
                var allDatProcessNumbers = vehicles
                    .SelectMany(v => v.DatProcessNumbers ?? new List<string>())
                    .Where(dpn => !string.IsNullOrWhiteSpace(dpn))
                    .Distinct()
                    .ToList();
                
                // Tüm araçların OEM kodlarını topla
                var allOemCodes = vehicles
                    .SelectMany(v => v.OemParts ?? new List<ecommerce.Admin.Services.Dtos.VinDto.VinOemPartDto>())
                    .Where(p => !string.IsNullOrWhiteSpace(p.Oem))
                    .Select(p => p.Oem.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", ""))
                    .Where(oem => !string.IsNullOrEmpty(oem))
                    .Distinct()
                    .ToList();
                
                // Tüm araçların üreticilerini topla
                var allManufacturers = vehicles
                    .Select(v => v.ManufacturerName)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct()
                    .ToList();
                
                Logger.LogInformation("[VIN-COUNT] Toplam DPN: {DpnCount}, Toplam OEM: {OemCount}, Markalar: {Brands}", 
                    allDatProcessNumbers.Count, allOemCodes.Count, string.Join(", ", allManufacturers));
                
                if (!allDatProcessNumbers.Any() && !allOemCodes.Any())
                {
                    Logger.LogWarning("[VIN-COUNT] Hiçbir araç için kod bulunamadı!");
                    return;
                }
                
                // TEK SORGU ile tüm ürünleri al - Üretici kısıtlaması EKLENDİ (alakasız muadilleri önlemek için)
                
                var vehicleManufacturerKeys = vehicles
                    .Select(v => v.ManufacturerKey)
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Distinct()
                    .ToList();
                
                // [VIN-FIX] Persist to class field for Grid Search
                vinManufacturerKeys = vehicleManufacturerKeys;

                var filter = new SearchFilterReguestDto
                {
                    DatProcessNumbers = allDatProcessNumbers.Any() ? allDatProcessNumbers : null,
                    OemCodes = !allDatProcessNumbers.Any() && allOemCodes.Any() ? allOemCodes : null,
                    Page = 1,
                    PageSize = 5000, // PERFORMANS: Yüksek limit (2500 -> 5000), tüm varyantların ürünlerini kapsasın
                    ManufacturerKeys = vehicleManufacturerKeys // [VIN-FIX] Server-side filtering
                };
                
                var result = await ProductSearchService.GetByFilterPagingAsync(filter);
                
                if (!result.Ok || result.Result == null || result.Result.Data == null)
                {
                    Logger.LogWarning("[VIN-COUNT] Elasticsearch sorgusu başarısız!");
                    return;
                }
                
                var allProducts = result.Result.Data;
                Logger.LogInformation("[VIN-COUNT] Toplam {Count} ürün bulundu", allProducts.Count);
                
                // Her araç için ürün sayısını hesapla
                // [VIN-COUNT-FIX] Use EXACT same filtering as the main search results 
                // to avoid discrepancies between modal and final list.
                int processedVehicles = 0;

                foreach (var vehicle in vehicles)
                {
                    processedVehicles++;
                    Logger.LogInformation("[VIN-COUNT] Araç {Current}/{Total} işleniyor: {Manufacturer} {Model}", 
                        processedVehicles, vehicles.Count, vehicle.ManufacturerName, vehicle.BaseModelName);

                    var vehicleKey = $"{vehicle.ManufacturerKey}_{vehicle.BaseModelKey}_{vehicle.Wmi}_{vehicle.VdsCode}_{vehicle.ModelYear}";
                    
                    // Bu aracın kodları
                    var vehicleDpns = (vehicle.DatProcessNumbers ?? new List<string>())
                        .Where(dpn => !string.IsNullOrWhiteSpace(dpn))
                        .Select(dpn => dpn.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    
                    var vehicleOems = (vehicle.OemParts ?? new List<ecommerce.Admin.Services.Dtos.VinDto.VinOemPartDto>())
                        .Where(p => !string.IsNullOrWhiteSpace(p.Oem))
                        .Select(p => p.Oem.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", ""))
                        .Where(oem => !string.IsNullOrEmpty(oem))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    
                    // Bu araca ait ürünleri filtrele - EXACT MATCH with Service logic
                    var matchingProducts = allProducts.Where(product =>
                    {
                        // 1. Manufacturer/Model Check (Strict)
                        if (!string.IsNullOrEmpty(product.ManufacturerKey) && 
                            !product.ManufacturerKey.Equals(vehicle.ManufacturerKey, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        // 2. DPN Match
                        bool hasDpnMatch = false;
                        if (vehicleDpns.Any() && product.DatProcessNumber != null && product.DatProcessNumber.Any())
                        {
                            if (product.DatProcessNumber.Any(pdpn => !string.IsNullOrWhiteSpace(pdpn) && vehicleDpns.Contains(pdpn.Trim())))
                            {
                                hasDpnMatch = true;
                            }
                        }
                        
                        // 3. OEM Match
                        bool hasOemMatch = false;
                        if (vehicleOems.Any())
                        {
                            // PartNumber check
                            if (!string.IsNullOrWhiteSpace(product.PartNumber))
                            {
                                var normalizedP = product.PartNumber.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
                                if (vehicleOems.Contains(normalizedP)) hasOemMatch = true;
                            }

                            // OemCode array check
                            if (!hasOemMatch && product.OemCode != null && product.OemCode.Any())
                            {
                                foreach (var oem in product.OemCode)
                                {
                                    var normalizedO = oem.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
                                    if (vehicleOems.Contains(normalizedO)) { hasOemMatch = true; break; }
                                }
                            }
                        }

                        // Hybrid Logic: Matches Service Should clause (DPN OR OEM)
                        return hasDpnMatch || hasOemMatch;
                    }).Count();
                    
                    vinVehicleProductCounts[vehicleKey] = matchingProducts;
                    
                    Logger.LogInformation("[VIN-COUNT] {Manufacturer} {Model} - {Count} ürün eşleşti", 
                        vehicle.ManufacturerName, vehicle.BaseModelName, matchingProducts);
                }
                
                Logger.LogInformation("[VIN-COUNT] Hesaplama tamamlandı - {Count} araç için ürün sayısı belirlendi", 
                    vinVehicleProductCounts.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VIN-COUNT] CalculateProductCountsForVehicles hatası");
            }
            finally
            {
                isCalculatingVinCounts = false;
            }
        }

        /// <summary>
        /// VIN araç seçim modalını kapatır
        /// </summary>
        protected void CloseVinVehicleSelectionModal()
        {
            isVinVehicleSelectionModalOpen = false;
            vinMatchedVehicles.Clear();
            vinVehicleProductCounts.Clear(); // Ürün sayılarını da temizle
            currentVinNumber = "";
            vinVehicleSearchText = ""; // Arama metnini temizle
            isLoading = false;
            StateHasChanged();
        }

        protected async Task ClearSearch()
        {
            searchText = "";
            hasSearched = false;
            searchResult.Data.Clear();
            brands.Clear();
            categories.Clear();
            dotPartNames.Clear();
            manufacturerNames.Clear();
            baseModelNames.Clear();
            subModelNames.Clear();
            compatibleVehicles.Clear();
            selectedCategoryIds.Clear();
            selectedBrandIds.Clear();
            selectedProductIds.Clear();
            selectedDotPartNames.Clear();
            selectedManufacturerNames.Clear();
            selectedBaseModelNames.Clear();
            selectedSubModelNames.Clear();
            selectedDatProcessNumbers.Clear();
            selectedOemCodes.Clear();
            StateHasChanged();
        }

        protected async Task OnLoadData(LoadDataArgs args)
        {
            isLoading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var page = (args.Skip / args.Top) + 1 ?? 1;
                var pageSize = args.Top ?? 50;

                await SearchProducts(preserveFilterOptions: true, page: page, pageSize: pageSize, triggerPaginationReset: false);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Veri Yükleme Hatası", ex.Message);
            }
            finally
            {
                isLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task SearchProducts(bool preserveFilterOptions = false, int page = 1, int pageSize = 50, bool triggerPaginationReset = true)
        {
            isLoading = true; // Ensure loading state is active
            _searchStartTime = DateTime.UtcNow; // Reset timer for ML tracking
            await InvokeAsync(StateHasChanged);
            
            // Fix: Do not return early. We removed LoadData binding in Razor, so we must execute the search manually.
            if (page == 1 && searchGrid != null && triggerPaginationReset)
            {
                await searchGrid.GoToPage(0);
                // Continue execution to load data
            }
            
            hasSearched = true;
            await InvokeAsync(StateHasChanged);
            
            try
            {
                var filter = new SearchFilterReguestDto
                {
                    Search = searchText,
                    OnlyInStock = showOnlyInStock,
                    OnlyWithImage = showOnlyWithImage,
                    CategoryIds = selectedCategoryIds.Any() ? selectedCategoryIds : null,
                    BrandIds = selectedBrandIds.Any() ? selectedBrandIds : null,
                    ProductIds = selectedProductIds.Any() ? selectedProductIds : null,
                    DotPartNames = selectedDotPartNames.Any() ? selectedDotPartNames.ToList() : null,
                    DatProcessNumbers = selectedDatProcessNumbers.Any() ? selectedDatProcessNumbers : null,
                    OemCodes = selectedOemCodes.Any() ? selectedOemCodes : null,
                    
                    // [VIN-FIX] Applied vehicle filters consistently to maintain context during pagination and filter toggles.
                    ManufacturerNames = selectedManufacturerNames.Any() 
                        ? selectedManufacturerNames.ToList() 
                        : (!string.IsNullOrEmpty(selectedManufacturerName) ? new List<string> { selectedManufacturerName } : null),
                        
                    BaseModelNames = selectedBaseModelNames.Any() 
                        ? selectedBaseModelNames.ToList() 
                        : (!string.IsNullOrEmpty(selectedBaseModelName) ? new List<string> { selectedBaseModelName } : null),
                        
                    SubModelNames = selectedSubModelNames.Any() 
                        ? selectedSubModelNames.ToList() 
                        : (!string.IsNullOrEmpty(selectedSubModelName) ? new List<string> { selectedSubModelName } : null),

                    MinPrice = minPrice.HasValue && minPrice.Value > 0 ? minPrice : (minPrice == null ? 0.01 : minPrice), 
                    MaxPrice = maxPrice,
                    
                    SingleManufacturerName = selectedManufacturerName,
                    SingleModelName = selectedBaseModelName,
                    SingleSubModelName = selectedSubModelName,
                    
                    SubModelKeys = string.IsNullOrEmpty(selectedSubModelKey) ? null : new List<string> { selectedSubModelKey },
                    Page = page,
                    PageSize = pageSize,
                    // [VIN-FIX] Apply ManufacturerKey restriction if available to keep search within the selected brand context
                    ManufacturerKeys = vinManufacturerKeys.Any() ? vinManufacturerKeys : null,
                    ShouldGroupOems = !isIncludeEquivalents
                };
                
                // [VIN-FIX] Safety Net: If functionality is "Originals Only" (!isIncludeEquivalents) 
                // but keys are missing (e.g. lost state), try to restore them from currentVinVehicle
                if (!isIncludeEquivalents && (vinManufacturerKeys == null || !vinManufacturerKeys.Any()) && currentVinVehicle != null && !string.IsNullOrEmpty(currentVinVehicle.ManufacturerKey))
                {
                     Logger.LogWarning("[VIN-DEBUG] ManufacturerKeys missing during strict search! Restoring from CurrentVinVehicle: {Key}", currentVinVehicle.ManufacturerKey);
                     vinManufacturerKeys = new List<string> { currentVinVehicle.ManufacturerKey };
                }

                Logger.LogInformation("[VIN-DEBUG] SearchProducts - IncludeEq: {Eq}, VinKeys: {Vk}, Filter.MKeys: {Fmk}, Filter.MNames: {Fmn}", 
                    isIncludeEquivalents,
                    vinManufacturerKeys != null ? string.Join(",", vinManufacturerKeys) : "null",
                    filter.ManufacturerKeys != null ? string.Join(",", filter.ManufacturerKeys) : "null",
                    filter.ManufacturerNames != null ? string.Join(",", filter.ManufacturerNames) : "null");

                // Fetch Products
                var result = await ProductSearchService.GetByFilterPagingAsync(filter);
                
                // Fetch Aggregations (Cascading Filters)
                var aggsResult = await ProductSearchService.GetSearchAggregationsAsync(filter);

                // Fetch Dynamic Metadata for Grouping Setting
                var metadata = await SynonymService.GetSearchMetadataAsync();

                if (result.Ok && result.Result != null)
                {
                    searchResult = result.Result;
                    hasSearched = true;
                    
                    if (searchResult.Data == null)
                    {
                        searchResult.Data = new List<SellerProductViewModel>();
                    }
                    
                    if (searchResult.Data != null && searchResult.Data.Any())
                    {
                        // The service already handles CorrectAndDeduplicate according to ShouldGroupOems setting.
                        // We only need to ensure the final sort order here: In-Stock items first, then by Price ASC
                        searchResult.Data = searchResult.Data
                            .OrderByDescending(x => x.Stock > 0)
                            .ThenBy(x => x.SalePrice)
                            .ToList();
                    }

                    isLoading = false;
                    ExtractFilters();
                    await LoadPurchasedProductIdsAsync();
                    await InvokeAsync(StateHasChanged);
                    
                    // Force grouping update after grid render
                     if (isGroupedBySeller && searchGrid != null)
                     {
                         // Small delay to allow Grid ID/Reference to update after re-render
                         await Task.Delay(50);
                         await UpdateGridGroups();
                     }
                    
                    if (searchResult.DataCount > 0 && (searchResult.Data == null || !searchResult.Data.Any()))
                    {
                        NotificationService.Notify(new NotificationMessage 
                        { 
                            Severity = NotificationSeverity.Warning, 
                            Summary = "Veri Yükleme Hatası", 
                            Detail = $"{searchResult.DataCount} sonuç bulundu ancak veriler yüklenemedi. Lütfen sayfayı yenileyin.",
                            Duration = 5000
                        });
                    }
                }
                else
                {
                    // Handle Error Case
                    Console.WriteLine($"[ERROR] Search Failed: {result.Metadata?.Message}");
                    hasSearched = true;
                    searchResult = new Paging<List<SellerProductViewModel>> { Data = new List<SellerProductViewModel>(), DataCount = 0 };
                    isLoading = false;
                    StateHasChanged();
                    
                    if(!string.IsNullOrEmpty(result.Metadata?.Message))
                    {
                        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Arama Hatası", Detail = result.Metadata.Message });
                    }
                }

                    if (!preserveFilterOptions)
                    {
                        ExtractFilters();
                    }

                    if(aggsResult.Ok && aggsResult.Result != null)
                    {
                        manufacturerNames = aggsResult.Result.Manufacturers.OrderBy(x => x).ToList();
                        baseModelNames = aggsResult.Result.BaseModels.OrderBy(x => x).ToList();
                        subModelNames = aggsResult.Result.SubModels.OrderBy(x => x).ToList();

                        // --- SMART MANUFACTURER FILTER ---
                        // If the search text matches a MODEL name exactly (or close enough), 
                        // we should filter the manufacturers to only show the ones relevant to that model.
                        // Example: User searches "Passat". Aggregations return "Passat" in baseModelNames.
                        // But Manufacturer aggregations might show "Fiat", "Ford" etc because of broad text match.
                        // We want to limit Manufacturer list to "Volkswagen" (and any others valid for Passat).
                        
                        if (!string.IsNullOrWhiteSpace(searchText) && baseModelNames.Any())
                        {
                            var normalizedSearch = searchText.Trim();
                            
                            // Check if search text matches any model name
                            var matchedModel = baseModelNames.FirstOrDefault(m => m.Equals(normalizedSearch, StringComparison.OrdinalIgnoreCase));
                            
                            if (matchedModel != null)
                            {
                                // Strategy: Look at the top results. If they are mostly from a specific manufacturer for this model, use that.
                                // Or better: Filter the current 'searchResult.Data' to see which manufacturers are actually associated with this Model in the results.
                                
                                var relevantManufacturers = searchResult.Data
                                    .Where(p => string.Equals(p.BaseModelName, matchedModel, StringComparison.OrdinalIgnoreCase))
                                    .Select(p => p.ManufacturerName)
                                    .Where(m => !string.IsNullOrEmpty(m))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                                if (relevantManufacturers.Any())
                                {
                                    // Intersect with the aggregation results to ensure valid options
                                    var smartFilteredManufacturers = manufacturerNames
                                        .Where(m => relevantManufacturers.Contains(m, StringComparer.OrdinalIgnoreCase))
                                        .OrderBy(x => x)
                                        .ToList();

                                    // Only apply if we found valid manufacturers, otherwise keep the broad list
                                    if (smartFilteredManufacturers.Any())
                                    {
                                        manufacturerNames = smartFilteredManufacturers;
                                    }
                                }
                            }
                        }
                        // ---------------------------------
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] SearchProducts Error: {ex.Message} \nStack: {ex.StackTrace}");
                // If error occurs, we should still behave as 'searched' to show empty state/error, not dashboard
                hasSearched = true; 
                searchResult = new Paging<List<SellerProductViewModel>> { Data = new List<SellerProductViewModel>(), DataCount = 0 };
                
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = ex.Message });
            }
            finally
            {
                // CRITICAL: Ensure isLoading is false
                if (isLoading)
                {
                    isLoading = false;
                }
                
                // Force UI update immediately - CRITICAL for rendering
                await InvokeAsync(StateHasChanged);
                
                // Double-check: Force another update after a tiny delay to ensure UI catches the state change
                await Task.Delay(50);
                await InvokeAsync(StateHasChanged);
                
                // Reapply grouping after data changes to ensure it persists - ONLY IF NOT ALREADY APPLIED
                // await Task.Delay(100); // Removed to prevent infinite LoadData trigger loop
                // if (searchGrid != null && searchResult != null && searchResult.Data != null && searchResult.Data.Any())
                // {
                //    await UpdateGridGroups();
                //    await InvokeAsync(StateHasChanged);
                // }
            }
        }

        private void ExtractFilters()
        {
            if (searchResult?.Data == null) return;

            // Extract unique brands
            brands = searchResult.Data
                .Where(p => p.Brand != null)
                .Select(p => p.Brand!)
                .GroupBy(b => b.Id)
                .Select(g => g.First())
                .ToList();

            // Extract unique categories
            categories = searchResult.Data
                .Where(p => p.Categories != null && p.Categories.Any())
                .SelectMany(p => p.Categories!)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .ToList();

            // Extract other filter options from products
            dotPartNames = searchResult.Data
                .Where(p => !string.IsNullOrEmpty(p.DotPartName))
                .Select(p => p.DotPartName!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Removed Incorrect Vehicle Data Extraction
            // Rely on Backend Aggregations (aggsResult) for Manufacturers/Models
            
            // Extract compatible vehicles list for the modal - NOTE: This might still show Brand as Manufacturer if ViewModel is ambiguous, 
            // but for filtering we trust the Aggregations.
            compatibleVehicles = searchResult.Data
                .Where(p => p.SubModelsJson != null && p.SubModelsJson.Any())
                .SelectMany(p => p.SubModelsJson!.Select(sm => new CompatibleVehicleDto
                {
                    ManufacturerName = p.ManufacturerName, 
                    BaseModelName = p.BaseModelName,
                    SubModelName = sm.Name
                }))
                .GroupBy(v => new { v.ManufacturerName, v.BaseModelName, v.SubModelName })
                .Select(g => g.First())
                .ToList();
        }

        protected async Task ToggleDotPartFilter(string name)
        {
            if (selectedDotPartNames.Contains(name)) selectedDotPartNames.Remove(name);
            else selectedDotPartNames.Add(name);
            await SearchProducts();
        }

        protected async Task ToggleManufacturerFilter(string name)
        {
            if (selectedManufacturerNames.Contains(name)) selectedManufacturerNames.Remove(name);
            else selectedManufacturerNames.Add(name);
            await SearchProducts();
        }

        protected async Task ToggleBaseModelFilter(string name)
        {
            if (selectedBaseModelNames.Contains(name)) selectedBaseModelNames.Remove(name);
            else selectedBaseModelNames.Add(name);
            await SearchProducts();
        }

        protected async Task ToggleSubModelFilter(string name)
        {
            if (selectedSubModelNames.Contains(name)) selectedSubModelNames.Remove(name);
            else selectedSubModelNames.Add(name);
            await SearchProducts();
        }

        protected async Task OnDotPartNamesChanged(object value)
        {
            await OnSearchFiltersChanged();
        }


        protected async Task OnOnlyInStockToggle(bool value)
        {
            showOnlyInStock = value;
            if (hasSearched)
            {
                await SearchProducts();
            }
        }

        protected async Task OnOnlyWithImageToggle(bool value)
        {
            showOnlyWithImage = value;
            if (hasSearched)
            {
                await SearchProducts();
            }
        }

        protected void SwitchTab(string tab)
        {
            activeTab = tab;
        }

        protected void ToggleView()
        {
            isTableView = !isTableView;
        }

        protected void GoToProduct(int sellerItemId)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Ürün Seçildi", Detail = $"SellerItemId: {sellerItemId}" });
        }

        protected string ResolveSearchImage(SellerProductViewModel product)
        {
            var cdnBaseUrl = Configuration["Cdn:BaseUrl"] ?? "https://cdn.yedeksen.com/images/";

            // 1. Try MainImageUrl (populated from Index or Join fallback)
            if (!string.IsNullOrEmpty(product.MainImageUrl))
                return $"{cdnBaseUrl}ProductImages/{product.MainImageUrl}";

            // 2. Try Images collection (double check)
            if (product.Images != null && product.Images.Any())
                 return $"{cdnBaseUrl}ProductImages/{product.Images.First().FileName}";

            // 3. Fallback to DocumentUrl (legacy)
            if (!string.IsNullOrEmpty(product.DocumentUrl))
                 return $"{cdnBaseUrl}products/{product.DocumentUrl}";

            // Always return no-photo.png if no image found (no error messages)
            return "/images/no-photo.png";
        }

        protected async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await OnSearchSubmit();
            }
        }

        protected async Task SearchByTerm(string term)
        {
            searchText = term;
            brands.Clear();
            categories.Clear();
            compatibleVehicles.Clear();
            
            if (string.IsNullOrEmpty(term))
            {
                searchResult.Data.Clear();
                StateHasChanged();
                return;
            }
            
            await SearchProducts();
        }

        protected async Task SearchByVehicle(string manufacturerName, string baseModelName, string subModelName)
        {
            var prevManufacturer = selectedManufacturerName;
            var prevBaseModel = selectedBaseModelName;
            var prevSubModel = selectedSubModelName;

            selectedManufacturerName = manufacturerName;
            selectedBaseModelName = baseModelName;
            selectedSubModelName = subModelName;
            
            await SearchProducts();

            if (searchResult.DataCount == 0)
            {
                NotificationService.Notify(new NotificationMessage 
                { 
                    Severity = NotificationSeverity.Warning, 
                    Summary = "Sonuç Bulunamadı", 
                    Detail = "Seçilen araç için uygun ürün bulunamadı. Filtre geri alınıyor.",
                    Duration = 4000
                });

                selectedManufacturerName = prevManufacturer;
                selectedBaseModelName = prevBaseModel;
                selectedSubModelName = prevSubModel;

                await SearchProducts();
            }
        }

        protected async Task ToggleCategoryFilter(int categoryId)
        {
            if (selectedCategoryIds.Contains(categoryId))
                selectedCategoryIds.Remove(categoryId);
            else
                selectedCategoryIds.Add(categoryId);
            
            await SearchProducts();
        }

        protected async Task ToggleBrandFilter(int brandId)
        {
            if (selectedBrandIds.Contains(brandId))
                selectedBrandIds.Remove(brandId);
            else
                selectedBrandIds.Add(brandId);
            
            await SearchProducts();
        }

        protected async Task ApplyPriceFilter()
        {
            await SearchProducts();
        }

        protected async Task ClearPriceFilter()
        {
            minPrice = null;
            maxPrice = null;
            await SearchProducts();
        }

        protected async Task ClearAllFilters()
        {
            selectedCategoryIds.Clear();
            selectedBrandIds.Clear();
            selectedDotPartNames.Clear();
            selectedDatProcessNumbers.Clear();
            selectedManufacturerNames.Clear();
            selectedBaseModelNames.Clear();
            selectedSubModelNames.Clear();
            selectedManufacturerName = null;
            selectedBaseModelName = null;
            selectedSubModelName = null;
            minPrice = null;
            maxPrice = null;
            showOnlyInStock = true;
            
            // User requested that clearing filters should hide the list and filter screen.
            // This effectively resets the page state.
            searchText = "";
            hasSearched = false;
            searchResult.Data.Clear();
            brands.Clear();
            categories.Clear();
            dotPartNames.Clear();
            manufacturerNames.Clear();
            baseModelNames.Clear();
            subModelNames.Clear();
            compatibleVehicles.Clear();
            StateHasChanged();
        }

        // Vehicle Match Modal Methods
        protected async Task OpenVehicleMatchModal()
        {
            isVehicleMatchModalOpen = true;
            await JSRuntime.InvokeVoidAsync("eval", "document.body.style.overflow = 'hidden'");
            await LoadManufacturersForModal();
            StateHasChanged();
        }

        protected async Task LoadManufacturersForModal()
        {
            try
            {
                // Use Aggregations instead of Product Search for performance and completeness
                // This ensures we get ALL manufacturers that match the filter, not just the ones in the first 500 results
                var filter = new SearchFilterReguestDto 
                { 
                    OnlyInStock = isModalOnlyInStock,
                    Page = 1, 
                    PageSize = 0 // We don't need product hits, just aggregations
                };
                
                var aggsResult = await ProductSearchService.GetSearchAggregationsAsync(filter);
                
                if (aggsResult.Ok && aggsResult.Result != null)
                {
                    modalManufacturers = aggsResult.Result.Manufacturers
                        .Where(x => !string.IsNullOrEmpty(x))
                        .OrderBy(x => x)
                        .ToList();
                }
                else
                {
                    modalManufacturers.Clear();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage 
                { 
                    Severity = NotificationSeverity.Error, 
                    Summary = "Hata", 
                    Detail = $"Araç verileri yüklenirken hata oluştu: {ex.Message}" 
                });
                modalManufacturers.Clear();
            }
        }

        protected async Task OnVehicleMatchStockSwitchChanged(bool value)
        {
            isModalOnlyInStock = value;
            
            // Clear current selections as the available data might change
            modalSelectedManufacturer = null;
            modalSelectedModel = null;
            modalSelectedSubModel = null;
            modalSelectedDotPart = null;
            modalSelectedDotPartProcessNumber = null;
            
            modalBaseModels.Clear();
            modalSubModels.Clear();
            modalDotParts.Clear();
            modalModelSubModelsCache.Clear();

            // Reload manufacturers with new stock filter
            await LoadManufacturersForModal();
            StateHasChanged();
        }

        protected async Task CloseVehicleMatchModal()
        {
            isVehicleMatchModalOpen = false;
            await JSRuntime.InvokeVoidAsync("eval", "document.body.style.overflow = 'auto'");
            modalSelectedManufacturer = null;
            modalSelectedModel = null;
            modalSelectedSubModel = null;
            modalSelectedDotPart = null;
            modalSelectedDotPartProcessNumber = null;
            isModalOnlyInStock = false;
            modalManufacturers.Clear(); // Clear independent list through method or access directly
            modalBaseModels.Clear();
            modalSubModels.Clear();
            modalDotParts.Clear();
            modalModelSubModelsCache.Clear();
            StateHasChanged();
        }

        protected async Task OnModalManufacturerChanged(string manufacturer)
        {
            modalSelectedManufacturer = manufacturer;
            modalSelectedModel = null;
            modalSelectedSubModel = null;
            modalSelectedDotPart = null;
            modalSelectedDotPartProcessNumber = null;
            modalBaseModels.Clear();
            modalSubModels.Clear();
            modalDotParts.Clear();
            modalModelSearch = "";
            modalSubModelSearch = "";
            modalDotPartSearch = "";
            
            isModelLoading = true;
            
            // Load models for selected manufacturer by searching products
            if (!string.IsNullOrEmpty(manufacturer))
            {
                try
                {
                    var filter = new SearchFilterReguestDto 
                    { 
                        ManufacturerNames = new List<string> { manufacturer },
                        OnlyInStock = isModalOnlyInStock,
                        Page = 1, 
                        PageSize = 500 // Get enough products to extract all models
                    };
                    
                    var productResult = await ProductSearchService.GetByFilterPagingAsync(filter);
                    
                    if (productResult.Ok && productResult.Result != null && productResult.Result.Data.Any())
                    {
                        // Extract unique model names from products
                        var models = productResult.Result.Data
                            .Select(p => p.BaseModelName)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(x => x)
                            .ToList();
                        
                        modalBaseModels = models!;
                        
                // Build model->submodels cache
                modalModelSubModelsCache.Clear();

                foreach (var product in productResult.Result.Data.Where(p => p.SubModelsJson != null && p.SubModelsJson.Any()))
                {
                    var modelName = product.BaseModelName;
                    if (string.IsNullOrEmpty(modelName)) continue;

                    if (!modalModelSubModelsCache.ContainsKey(modelName))
                    {
                        modalModelSubModelsCache[modelName] = new List<SubModelDto>();
                    }

                    foreach (var sm in product.SubModelsJson)
                    {
                        if (sm != null && !string.IsNullOrEmpty(sm.Key) && !string.IsNullOrWhiteSpace(sm.Name) &&
                            !modalModelSubModelsCache[modelName].Any(x => x.Key == sm.Key))
                        {
                            modalModelSubModelsCache[modelName].Add(sm);
                        }
                    }
                }
                        
                        
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(new NotificationMessage 
                    { 
                        Severity = NotificationSeverity.Error, 
                        Summary = "Hata", 
                        Detail = $"Model verileri yüklenirken hata oluştu: {ex.Message}" 
                    });
                }
            }
            
            isModelLoading = false;
            StateHasChanged();
        }

        protected async Task OnModalModelChanged(string model)
        {
            modalSelectedModel = model;
            modalSelectedSubModel = null;
            modalSelectedDotPart = null;
            modalSelectedDotPartDisplay = null;
            modalSelectedDotPartProcessNumber = null;
            modalSubModels.Clear();
            modalDotParts.Clear();
            modalSubModelSearch = "";
            modalDotPartSearch = "";
            
            isSubModelLoading = true;
            
            // Load submodels from cache first
            if (!string.IsNullOrEmpty(model))
            {
                if (modalModelSubModelsCache.TryGetValue(model, out var cachedSubModels))
                {
                    modalSubModels = cachedSubModels.OrderBy(x => x.Name).ToList();
                    isSubModelLoading = false;
                    StateHasChanged();
                    return;
                }
            }
            
            // Fallback: Load submodels by searching for products with this manufacturer and model
            if (!string.IsNullOrEmpty(modalSelectedManufacturer) && !string.IsNullOrEmpty(model))
            {
                try
                {
                    var filter = new SearchFilterReguestDto 
                    { 
                        ManufacturerNames = new List<string> { modalSelectedManufacturer },
                        BaseModelNames = new List<string> { model },
                        OnlyInStock = isModalOnlyInStock,
                        Page = 1, 
                        PageSize = 500 
                    };
                    
                    var productResult = await ProductSearchService.GetByFilterPagingAsync(filter);
                    
                    if (productResult.Ok && productResult.Result != null && productResult.Result.Data.Any())
                    {
                        // Extract unique submodel objects from products
                        var subModels = productResult.Result.Data
                            .Where(p => p.SubModelsJson != null && p.SubModelsJson.Any())
                            .SelectMany(p => p.SubModelsJson!)
                            .Where(sm => sm != null && !string.IsNullOrEmpty(sm.Key) && !string.IsNullOrWhiteSpace(sm.Name))
                            .DistinctBy(sm => sm.Key)
                            .OrderBy(x => x.Name)
                            .ToList();
                        
                        modalSubModels = subModels;
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(new NotificationMessage 
                    { 
                        Severity = NotificationSeverity.Error, 
                        Summary = "Hata", 
                        Detail = $"Alt model verileri yüklenirken hata oluştu: {ex.Message}" 
                    });
                }
            }
            
            isSubModelLoading = false;
            StateHasChanged();
        }

        protected async Task OnModalSubModelChanged(string? subModel)
        {
            modalSelectedSubModel = subModel;
            modalSelectedDotPart = null;
            modalSelectedDotPartDisplay = null;
            modalSelectedDotPartProcessNumber = null;
            modalDotParts.Clear();
            modalDotPartSearch = "";

            isDotPartLoading = true;

            if (!string.IsNullOrEmpty(subModel))
            {
                try
                {
                     // Search for products with this vehicle to extract DotPartNames
                     // Rely on strict hierarchy: Manufacturer -> Model -> SubModel
                     var filter = new SearchFilterReguestDto 
                     { 
                         ManufacturerNames = !string.IsNullOrEmpty(modalSelectedManufacturer) ? new List<string> { modalSelectedManufacturer } : null,
                         BaseModelNames = !string.IsNullOrEmpty(modalSelectedModel) ? new List<string> { modalSelectedModel } : null,
                         SubModelKeys = new List<string> { subModel },
                         OnlyInStock = false, // Fetch regardless of stock to get full list of parts
                         Page = 1, 
                         PageSize = 500 // Get more products to ensure we cover all potential groups
                     };
                     
                     var productResult = await ProductSearchService.GetByFilterPagingAsync(filter);
                     
                     if (productResult.Ok && productResult.Result != null && productResult.Result.Data.Any())
                     {
                           modalDotParts = productResult.Result.Data
                               .Where(p => !string.IsNullOrEmpty(p.DotPartName) || p.DatProcessNumber?.Any() == true)
                               .GroupBy(p => p.DotPartName ?? (p.DatProcessNumber != null ? string.Join(",", p.DatProcessNumber) : ""))
                               .Select(g => {
                                   var first = g.First();
                                   var displayName = !string.IsNullOrWhiteSpace(first.DotPartDescription) ? first.DotPartDescription : (first.DotPartName ?? (first.DatProcessNumber != null ? string.Join(", ", first.DatProcessNumber) : ""));
                                   var name = first.DotPartName ?? (first.DatProcessNumber != null ? string.Join(",", first.DatProcessNumber) : ""); // Fallback for filtering
                                   
                                   return new ModalDotPartDto 
                                   { 
                                       Name = name!, 
                                       DisplayName = displayName!,
                                       ProcessNumber = first.DatProcessNumber 
                                   };
                               })
                               .OrderBy(x => x.DisplayName)
                               .ToList();
                     }
                }
                catch (Exception)
                {
                     // ignore logging
                }
            }

            isDotPartLoading = false;
            StateHasChanged();
        }

        protected Task OnModalDotPartChanged(ModalDotPartDto dotPart)
        {
            modalSelectedDotPart = dotPart.Name;
            modalSelectedDotPartDisplay = dotPart.DisplayName;
            modalSelectedDotPartProcessNumber = dotPart.ProcessNumber;
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected async Task ApplyVehicleMatchFilters()
        {
            // Clear other search criteria to avoid conflicts
            searchText = "";
            searchResult.Data.Clear(); // Clear previous results
            selectedCategoryIds.Clear();
            selectedBrandIds.Clear();
            selectedDotPartNames.Clear();
            selectedDatProcessNumbers.Clear(); // Clear the new list
            selectedManufacturerNames.Clear(); // Clear multi-select filters
            selectedBaseModelNames.Clear();
            selectedSubModelNames.Clear();
            
            // Apply only vehicle filters (no categories - they're in sidebar)
            selectedManufacturerName = modalSelectedManufacturer;
            selectedBaseModelName = modalSelectedModel;
            selectedSubModelKey = modalSelectedSubModel;
            showOnlyInStock = isModalOnlyInStock;
            
            // Resolve name for display
            if (!string.IsNullOrEmpty(selectedSubModelKey))
            {
                 var sm = modalSubModels.FirstOrDefault(x => x.Key == selectedSubModelKey);
                 selectedSubModelName = sm?.Name;
            }
            else
            {
                selectedSubModelName = null;
            }

            // Apply Part Group Filter if selected
            if (!string.IsNullOrEmpty(modalSelectedDotPart))
            {
                // Prioritize DatProcessNumber if available
                if (modalSelectedDotPartProcessNumber?.Any() == true)
                {
                    selectedDatProcessNumbers.AddRange(modalSelectedDotPartProcessNumber);
                    selectedDotPartNames.Add(modalSelectedDotPart); 
                }
                else
                {
                    selectedDotPartNames.Add(modalSelectedDotPart);
                }
            }
            
            // Close modal
            await CloseVehicleMatchModal();
            
            // Perform search with new filters
            await SearchProducts();

            if (searchResult.DataCount == 0)
            {
                NotificationService.Notify(new NotificationMessage 
                { 
                    Severity = NotificationSeverity.Warning, 
                    Summary = "Tam Eşleşme Bulunamadı", 
                    Detail = "Seçilen tip için ürün bulunamadı. Marka ve Model bazlı sonuçlar listeleniyor...",
                    Duration = 4000
                });

                // Fallback: Remove SubModel Key filter and search again
                selectedSubModelKey = null;
                // Keep the name responsible for UI display ?? No, clear it so user knows filter dropped
                // But maybe keep it in a "searched for" label?
                // For now, clear it to reflect the actual search state
                selectedSubModelName = null;

                await SearchProducts();
            }
        }

        protected async Task OpenSimilarProductsModal(SellerProductViewModel product)
        {
            // Gather all possible codes: existing OemCode list + the primary PartNumber
            var oemCodes = (product.OemCode ?? new List<string>()).ToList();
            var partNumber = product.PartNumber?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(partNumber) && !oemCodes.Contains(partNumber))
            {
                oemCodes.Add(partNumber);
            }

            if (!oemCodes.Any()) return;

            await DialogService.OpenAsync<SimilarProductsModal>($"Muadil Ürünler: {product.PartNumber}",
                new Dictionary<string, object> { 
                    { "OemCodes", oemCodes },
                    { "SellerId", product.SellerId },
                    { "ExcludedProductSellerItemIds", searchResult.Data.Select(p => p.SellerItemId).ToList() }
                },
                new DialogOptions { Width = "900px", Height = "500px", Resizable = true, Draggable = true });
        }
        
        #region Cart Functionality
        
        [Inject] protected ecommerce.Admin.Services.Interfaces.ICartStateService CartStateService { get; set; } = default!;
        [Inject] protected ecommerce.Admin.Domain.Interfaces.IOrderService OrderService { get; set; } = default!;
        
        // Cart functionality
        protected Dictionary<int, int> productQuantities = new();
        protected Dictionary<int, int> existingCartItems = new(); // Stores actual cart quantities
        protected bool isCartVisible = false;

        // Geçmiş alışveriş: seçili cari için listedeki hangi ürünlerin geçmiş siparişi var
        protected HashSet<int> purchasedProductIds = new();
        protected bool ShowPurchaseHistoryColumn => (Security?.SelectedCustomerId ?? Security?.User?.CustomerId ?? 0) > 0;
        
        // Dashboard data
        protected bool isLoadingDashboard = false;
        protected int pendingOrderCount = 0;
        protected decimal pendingOrderTotal = 0m;
        protected int totalOrderCount = 0;
        protected decimal totalOrderAmount = 0m;
        protected decimal balance = 0m; // Placeholder for balance (debt/receivables)

        
        protected async Task LoadCartItems()
        {
             try
             {
                 var result = await CartService.GetCart();
                 
                 if (result.Ok && result.Result != null)
                 {
                     existingCartItems = result.Result.Sellers
                         .SelectMany(s => s.Items)
                         .ToDictionary(i => i.ProductSellerItemId, i => i.Quantity);
                         
                     // Sync product quantities with cart items
                     foreach (var item in existingCartItems)
                     {
                         productQuantities[item.Key] = item.Value;
                     }
                         
                     StateHasChanged();
                 }
             }
             catch (Exception ex)
             {
                 // Log error silently
             }
        }
        
        protected void RowRender(RowRenderEventArgs<SellerProductViewModel> args)
        {
            var classes = new List<string>();



            // Priority 1: Products in cart (light green)
            if (existingCartItems.ContainsKey(args.Data.SellerItemId))
            {
                args.Attributes.Add("style", "background-color: #d1e7dd; --rz-grid-hover-background-color: #c3e6cb;");
                classes.Add("cart-item-row");
            }
            // Priority 2: Out of stock or low stock (light red)
            else if (args.Data.Stock <= 0 || args.Data.Stock < 5)
            {
                args.Attributes.Add("style", "background-color: #f8d7da; --rz-grid-hover-background-color: #f1c5c9;");
                classes.Add("low-stock-row");
            }

            // Equivalent Product Highlight (Green left border)
            if (args.Data.IsEquivalent)
            {
                var currentStyle = args.Attributes.ContainsKey("style") ? args.Attributes["style"].ToString() : "";
                args.Attributes["style"] = $"{currentStyle} border-left: 4px solid #198754 !important;";
            }

            if (classes.Any())
            {
                args.Attributes.Add("class", string.Join(" ", classes));
            }
        }
        
        private bool IsCartRoleAllowed()
        {
            if (Security?.User?.Roles == null) return false;
            
            var isPlasiyer = Security.User.Roles.Any(r => r.Name == "Plasiyer");
            if (isPlasiyer)
            {
                // Plasiyer can only add to cart if they have selected a customer
                return Security.SelectedCustomerId.HasValue;
            }
            
            return Security.User.Roles.Any(r => r.Name == "CustomerB2B");
        }

        protected bool IsPlasiyer()
        {
            return Security.User?.Roles.Any(r => r.Name == "Plasiyer") ?? false;
        }

        protected bool IsCustomerB2B()
        {
            return Security.User?.Roles.Any(r => r.Name == "CustomerB2B") ?? false;
        }


        
        // Optimistic UI & Loading States
        protected Dictionary<int, bool> productLoadingStates = new();

        protected bool IsProductLoading(int sellerItemId) => productLoadingStates.ContainsKey(sellerItemId) && productLoadingStates[sellerItemId];

        protected Task UpdateQtyDifference(int sellerItemId, int diff)
        {
            var current = GetQuantity(sellerItemId);
            var next = current + diff;
            
            // Check limits again
            var product = searchResult.Data.FirstOrDefault(p => p.SellerItemId == sellerItemId);
            // Allow 0 for removal!
            if (next < 0) next = 0; 
            
            if (product != null && next > product.Stock)
            {
                 next = product.Stock;
                 NotificationService.Notify(NotificationSeverity.Warning, "Stok Sınırı", $"Stok limitine ({product.Stock}) ulaşıldı.", duration: 2000);
            }
            
            // Update local state
            productQuantities[sellerItemId] = next;
            
            // Trigger Add/Update logic (which handles the API call)
            // Note: AddToCart uses 'productQuantities[id]' as the target value.
            return AddToCart(sellerItemId);
        }


        // Loading states for modal cart operations
        private HashSet<int> modalLoadingItems = new();
        
        // Modal refresh counter for forcing re-render
        private int modalRefreshKey = 0;
        
        // Cart state change handler for modal

        protected async Task AddToCartFromModal(int productSellerItemId)
        {
            if (modalLoadingItems.Contains(productSellerItemId)) return;
            
            modalLoadingItems.Add(productSellerItemId);
            await InvokeAsync(StateHasChanged);
            
            try
            {
                // Get current cart quantity from CartStateService
                int currentCartQty = existingCartItems.GetValueOrDefault(productSellerItemId, 0);
                
                // Target quantity is 1 for new items
                int targetQty = 1;
                int delta = targetQty - currentCartQty;

                if (delta == 0)
                {
                    modalLoadingItems.Remove(productSellerItemId);
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                var req = new ecommerce.Web.Domain.Dtos.Cart.CartItemUpsertDto 
                { 
                    ProductSellerItemId = productSellerItemId, 
                    Quantity = delta,
                    CustomerId = Security?.SelectedCustomerId
                };
                
                var result = await CartService.CreateCartItem(req);
                
                if (result.Ok && result.Result != null)
                {
                    // Use returned cart directly (like CartDrawer)
                    CartStateService.SetCart(result.Result);
                    
                    // Update existingCartItems
                    if (result.Result.Sellers != null)
                    {
                        existingCartItems = result.Result.Sellers
                            .SelectMany(s => s.Items)
                            .ToDictionary(i => i.ProductSellerItemId, i => i.Quantity);
                    }
                    
                    // Force UI refresh - modalRefreshKey will trigger re-render via CartStateService.OnChange
                    modalRefreshKey++;
                    await InvokeAsync(StateHasChanged);
                    
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Sepete Eklendi",
                        Detail = "Ürün sepete eklendi.",
                        Duration = 2000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = result.Metadata?.Message ?? "Ürün sepete eklenemedi.",
                        Duration = 4000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = ex.Message,
                    Duration = 4000
                });
            }
            finally
            {
                modalLoadingItems.Remove(productSellerItemId);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task UpdateQuantityInModal(int productSellerItemId, int newQuantity)
        {
            if (modalLoadingItems.Contains(productSellerItemId)) return;
            
            // If quantity is 0 or less, remove item (like CartDrawer)
            if (newQuantity <= 0)
            {
                await RemoveItemFromModal(productSellerItemId);
                return;
            }
            
            modalLoadingItems.Add(productSellerItemId);
            await InvokeAsync(StateHasChanged);
            
            try
            {
                // Get current cart item to find itemId for removal if needed
                var currentCart = CartStateService.CurrentCart;
                var currentCartItem = currentCart?.Sellers?
                    .SelectMany(s => s.Items)
                    .FirstOrDefault(i => i.ProductSellerItemId == productSellerItemId);
                
                // Get current cart quantity
                int currentCartQty = existingCartItems.GetValueOrDefault(productSellerItemId, 0);
                
                // Calculate delta (like CartDrawer)
                int delta = newQuantity - currentCartQty;
                
                if (delta == 0)
                {
                    modalLoadingItems.Remove(productSellerItemId);
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                var req = new ecommerce.Web.Domain.Dtos.Cart.CartItemUpsertDto 
                { 
                    ProductSellerItemId = productSellerItemId, 
                    Quantity = delta,
                    CustomerId = Security?.SelectedCustomerId
                };
                
                var result = await CartService.CreateCartItem(req);
                
                if (result.Ok && result.Result != null)
                {
                    // Use returned cart directly (like CartDrawer)
                    CartStateService.SetCart(result.Result);
                    
                    // Update existingCartItems
                    if (result.Result.Sellers != null)
                    {
                        existingCartItems = result.Result.Sellers
                            .SelectMany(s => s.Items)
                            .ToDictionary(i => i.ProductSellerItemId, i => i.Quantity);
                    }
                    
                    // Force UI refresh - modalRefreshKey will trigger re-render via CartStateService.OnChange
                    modalRefreshKey++;
                    await InvokeAsync(StateHasChanged);
                    
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Güncellendi",
                        Detail = "Sepet güncellendi.",
                        Duration = 2000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = result.Metadata?.Message ?? "Miktar güncellenemedi.",
                        Duration = 4000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = ex.Message,
                    Duration = 4000
                });
            }
            finally
            {
                modalLoadingItems.Remove(productSellerItemId);
                await InvokeAsync(StateHasChanged);
            }
        }
        
        protected async Task RemoveItemFromModal(int productSellerItemId)
        {
            if (modalLoadingItems.Contains(productSellerItemId)) return;
            
            modalLoadingItems.Add(productSellerItemId);
            await InvokeAsync(StateHasChanged);
            
            try
            {
                // Find cart item ID
                var currentCart = CartStateService.CurrentCart;
                var cartItem = currentCart?.Sellers?
                    .SelectMany(s => s.Items)
                    .FirstOrDefault(i => i.ProductSellerItemId == productSellerItemId);
                
                if (cartItem == null)
                {
                    modalLoadingItems.Remove(productSellerItemId);
                    await InvokeAsync(StateHasChanged);
                    return;
                }
                
                var result = await CartService.CartItemRemove(cartItem.Id);
                
                if (result.Ok && result.Result != null)
                {
                    // Use returned cart directly (like CartDrawer)
                    CartStateService.SetCart(result.Result);
                    
                    // Update existingCartItems
                    if (result.Result.Sellers != null)
                    {
                        existingCartItems = result.Result.Sellers
                            .SelectMany(s => s.Items)
                            .ToDictionary(i => i.ProductSellerItemId, i => i.Quantity);
                    }
                    else
                    {
                        // Remove from dictionary if cart is empty
                        existingCartItems.Remove(productSellerItemId);
                    }
                    
                    // Force UI refresh - modalRefreshKey will trigger re-render via CartStateService.OnChange
                    modalRefreshKey++;
                    await InvokeAsync(StateHasChanged);
                    
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Info,
                        Summary = "Silindi",
                        Detail = "Ürün sepetten kaldırıldı.",
                        Duration = 2000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = result.Metadata?.Message ?? "Ürün sepetten kaldırılamadı.",
                        Duration = 4000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = ex.Message,
                    Duration = 4000
                });
            }
            finally
            {
                modalLoadingItems.Remove(productSellerItemId);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task AddToCart(int productSellerItemId)
        {
            if (IsProductLoading(productSellerItemId)) return;

            productLoadingStates[productSellerItemId] = true;
            StateHasChanged();
            
            try
            {
                // ML Instrumentation: Log AddToCart with Field Tracking
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var analyticsProduct = searchResult.Data.FirstOrDefault(p => p.SellerItemId == productSellerItemId);
                    if (analyticsProduct != null)
                    {
                        var rank = searchResult.Data.IndexOf(analyticsProduct) + 1;
                        
                        // Use service to detect matched fields (DRY principle)
                        var fieldMatch = FieldMatcherService.DetectMatchedFields(searchText, analyticsProduct);
                        
                        _ = SearchAnalyticsService.LogInteractionAsync(new ecommerce.Domain.Shared.Dtos.Search.SearchInteractionDto
                        {
                            SearchTerm = searchText,
                            ProductId = productSellerItemId,
                            InteractionType = "AddToCart",
                            Rank = rank,
                            UserId = Security?.User?.Id,
                            MatchedFields = fieldMatch.MatchedFields,
                            FieldScores = fieldMatch.FieldScores,
                            PrimaryMatchField = fieldMatch.PrimaryMatchField
                        });
                    }
                }

                // Get current cart quantity
                int currentCartQty = existingCartItems.GetValueOrDefault(productSellerItemId, 0);
                
                // Get target quantity from local state or default to 1
                int targetQty = productQuantities.GetValueOrDefault(productSellerItemId, 1);
                
                // FIX: If we're trying to add an item that has 0 qty in local state (previously removed),
                // or if we're adding it for the first time, ensure we add at least 1.
                if (currentCartQty == 0 && targetQty <= 0)
                {
                    targetQty = 1;
                    productQuantities[productSellerItemId] = 1;
                }

                // Calculate delta (like CartPage does)
                int delta = targetQty - currentCartQty;

                // If no change needed, return
                if (delta == 0) 
                {
                    productLoadingStates[productSellerItemId] = false;
                    StateHasChanged();
                    return;
                }

                // Check stock limit
                var product = searchResult.Data.FirstOrDefault(p => p.SellerItemId == productSellerItemId);
                if (product != null)
                {
                    if (targetQty > product.Stock)
                    {
                        targetQty = product.Stock;
                        productQuantities[productSellerItemId] = targetQty;
                        delta = targetQty - currentCartQty;
                        if (delta == 0)
                        {
                            productLoadingStates[productSellerItemId] = false;
                            StateHasChanged();
                            return;
                        }
                        NotificationService.Notify(NotificationSeverity.Warning, "Stok Sınırı", $"Adet stok limitine ({product.Stock}) çekildi.", duration: 2000);
                    }
                }

                // Create cart item request (like CartPage)
                var req = new ecommerce.Web.Domain.Dtos.Cart.CartItemUpsertDto 
                { 
                    ProductSellerItemId = productSellerItemId, 
                    Quantity = delta,
                    CustomerId = Security?.SelectedCustomerId
                };
                
                var result = await CartService.CreateCartItem(req);
                
                if (result.Ok && result.Result != null)
                {
                    // Use returned cart directly (like CartDrawer)
                    CartStateService.SetCart(result.Result);
                    
                    // Update local existingCartItems from updated cart
                    if (result.Result.Sellers != null)
                    {
                        existingCartItems = result.Result.Sellers
                            .SelectMany(s => s.Items)
                            .ToDictionary(i => i.ProductSellerItemId, i => i.Quantity);
                    }
                    
                    // Reload grid to update row styling
                    if (searchGrid != null)
                    {
                        await searchGrid.Reload();
                    }
                    
                    // Force UI update
                    StateHasChanged();
                    
                    // Show appropriate notification (like CartPage)
                    var productName = product?.ProductName ?? "Ürün";
                    var message = result.Metadata?.Message;
                    
                    if (string.IsNullOrEmpty(message))
                    {
                        message = "Sepet başarıyla güncellendi";
                    }
                    
                    NotificationService.Notify(NotificationSeverity.Success, "Sepet", message, duration: 2000);
                }
                else if (result.Ok)
                {
                    // Fallback: RefreshCart if result doesn't include cart
                    await CartStateService.RefreshCart();
                    
                    if (CartStateService.CurrentCart?.Sellers != null)
                    {
                        existingCartItems = CartStateService.CurrentCart.Sellers
                            .SelectMany(s => s.Items)
                            .ToDictionary(i => i.ProductSellerItemId, i => i.Quantity);
                    }
                    
                    // Reload grid to update row styling
                    if (searchGrid != null)
                    {
                        await searchGrid.Reload();
                    }
                    
                    // Force UI update
                    StateHasChanged();
                    
                    NotificationService.Notify(NotificationSeverity.Success, "Sepet", "Sepet başarıyla güncellendi", duration: 2000);
                }
                else
                {
                    var errMsg = result.Metadata?.Message ?? "Sepet güncellenirken hata oluştu";
                    NotificationService.Notify(NotificationSeverity.Error, "Sepet Hatası", errMsg, duration: 3000);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Beklenmedik hata", ex.Message, duration: 3000);
            }
            finally
            {
                productLoadingStates[productSellerItemId] = false;
                StateHasChanged();
            }
        }
        
        protected async Task OnQuantityChanged(int productSellerItemId, int value)
        {
            var product = searchResult.Data.FirstOrDefault(p => p.SellerItemId == productSellerItemId);
            
            int val = Math.Max(1, value);

            if (product != null && val > product.Stock)
            {
                 val = product.Stock;
                 NotificationService.Notify(NotificationSeverity.Warning, "Stok Sınırı", $"Adet stok limitine ({val}) çekildi.", duration: 2000);
            }

            productQuantities[productSellerItemId] = val;
            await AddToCart(productSellerItemId);
        }
        
        protected int GetQuantity(int productSellerItemId)
        {
            // If we have a manual entry, use it.
            if (productQuantities.TryGetValue(productSellerItemId, out var qty)) return qty;
            
            // Otherwise default to 1 (for new items) 
            return 1;
        }

        #endregion
        
        // Card view cart helpers
        private int GetCartQuantity(int sellerItemId)
        {
            return existingCartItems.GetValueOrDefault(sellerItemId, 0);
        }
        
        private async Task UpdateCartQuantityDelta(int sellerItemId, int delta)
        {
            if (IsProductLoading(sellerItemId)) return;
            
            productLoadingStates[sellerItemId] = true;
            StateHasChanged();
            
            try
            {
                var req = new ecommerce.Web.Domain.Dtos.Cart.CartItemUpsertDto 
                { 
                    ProductSellerItemId = sellerItemId, 
                    Quantity = delta,
                    CustomerId = Security?.SelectedCustomerId
                };
                
                var result = await CartService.CreateCartItem(req);
                
                if (result.Ok)
                {
                    await LoadCartItems();
                    StateHasChanged();
                    
                    string message = delta > 0 ? "Sepete eklendi." : "Sepet güncellendi.";
                    NotificationService.Notify(NotificationSeverity.Success, "Başarılı", message, duration: 2000);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Hata", result.Metadata?.Message ?? "Sepet güncellenemedi.", duration: 3000);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message, duration: 3000);
            }
            finally
            {
                productLoadingStates[sellerItemId] = false;
                StateHasChanged();
            }
        }
        
        private async Task OnCardQuantityChange(int sellerItemId, ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newQty) && newQty > 0)
            {
                var currentQty = GetCartQuantity(sellerItemId);
                if (newQty == currentQty) return;
                
                var delta = newQty - currentQty;
                await UpdateCartQuantityDelta(sellerItemId, delta);
            }
        }
        
        private async void OnCartStateChanged()
        {
            try
            {
                await LoadCartItems();
                
                var userId = Security?.User?.Id ?? 0;
                if (userId > 0)
                {
                    // Invalidate and refresh dashboard cache when cart changes
                    DashboardCacheService.InvalidateCache(userId);
                    await LoadDashboardData();
                }
                
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                // Log error silently
            }
        }
        


    private bool isIncludeEquivalents = false;

    private async Task ToggleIncludeEquivalents()
    {
        isIncludeEquivalents = !isIncludeEquivalents;
        isLoading = true;
        await InvokeAsync(StateHasChanged);
        await SearchProducts();
    }
    
    protected async Task ToggleGroupBySeller()
    {
        isGroupedBySeller = !isGroupedBySeller;
        await UpdateGridGroups();
        StateHasChanged();
    }

    protected async Task ClearSelectedCustomer()
    {
        await Security.SetSelectedCustomer(null, null);
        
        // Navigate to dashboard (product search home)
        Navigation.NavigateTo("/product-search", forceLoad: true);
    }


        protected async Task UpdateGridGroups()
        {
            if (searchGrid == null) return;
            
            // Remove existing SellerName grouping
            var existingGroup = searchGrid.Groups.FirstOrDefault(g => g.Property == "SellerName");
            if (existingGroup != null)
            {
                searchGrid.Groups.Remove(existingGroup);
            }
            
            // Add grouping if enabled
            if (isGroupedBySeller)
            {
                searchGrid.Groups.Add(new GroupDescriptor 
                { 
                    Property = "SellerName", 
                    Title = "Satıcı"
                });
            }
            
            // Force grid to refresh
            if (searchGrid != null)
            {
                await Task.Delay(10);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task ApplyDiscountSearch(ecommerce.Admin.Domain.Dtos.DiscountDto.DiscountWithProductsDto discount)
        {
            try
            {
                // Clear previous search
                searchText = "";
                
                // Reset filters
                selectedCategoryIds.Clear();
                selectedBrandIds.Clear();
                selectedProductIds.Clear();
                selectedManufacturerName = null;
                selectedBaseModelName = null;
                selectedSubModelName = null;
                selectedSubModelKey = null;
                selectedManufacturerNames.Clear();
                selectedBaseModelNames.Clear();
                selectedSubModelNames.Clear();
                selectedDotPartNames.Clear();
                selectedDatProcessNumbers.Clear();
                minPrice = null;
                maxPrice = null;
                
                // Set hasSearched to true to show results area
                hasSearched = true;
                
                // Set filters based on DiscountType
                switch (discount.DiscountType)
                {
                    case DiscountType.AssignedToProducts:
                        if (discount.AssignedEntityIds != null && discount.AssignedEntityIds.Any())
                        {
                            selectedProductIds = new List<int>(discount.AssignedEntityIds);
                        }
                        break;
                        
                    case DiscountType.AssignedToCategories:
                        // Kategori ID'lerini set et
                        if (discount.AssignedEntityIds != null && discount.AssignedEntityIds.Any())
                        {
                            selectedCategoryIds = new List<int>(discount.AssignedEntityIds);
                        }
                        break;
                        
                    case DiscountType.AssignedToBrands:
                        // Marka ID'lerini set et
                        if (discount.AssignedEntityIds != null && discount.AssignedEntityIds.Any())
                        {
                            selectedBrandIds = new List<int>(discount.AssignedEntityIds);
                        }
                        break;
                }
                
                // Arama yap
                isLoading = true;
                await SearchProducts();
                
                // Scroll to results
                await JSRuntime.InvokeVoidAsync("eval", "window.scrollTo({ top: document.querySelector('.product-results-area')?.offsetTop || 0, behavior: 'smooth' })");
            }
            catch (Exception)
            {
                isLoading = false;
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Kampanya ürünleri yüklenirken bir hata oluştu.",
                    Duration = 4000
                });
            }
        }

        /// <summary>
        /// Girilen metnin VIN numarası olup olmadığını kontrol eder
        /// </summary>
        protected bool IsVinNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var trimmed = text.Trim().ToUpperInvariant();
            
            // VIN 17 karakter olmalı ve sadece belirli karakterler içermeli (I, O, Q hariç)
            return trimmed.Length == 17 && 
                   System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[A-HJ-NPR-Z0-9]{17}$");
        }

        /// <summary>
        /// Arama input'u için CSS class'ı döndürür (VIN tespiti için)
        /// </summary>
        protected string GetSearchInputClass()
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return "b2b-search-input flex-grow-1";

            if (IsVinNumber(searchText))
                return "b2b-search-input flex-grow-1 vin-detected";

            return "b2b-search-input flex-grow-1";
        }

        public void Dispose()
        {
            if (CartStateService != null)
            {
                CartStateService.OnChange -= OnCartStateChanged;
            }

            if (Security != null)
            {
                Security.OnSelectedCustomerChanged -= OnSelectedCustomerChanged;
            }
        }

        // Dictionary to store ElementReferences by ID
        protected Dictionary<string, ElementReference> _elementReferences = new();

        protected void ShowTooltip(string elementId, string text)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(elementId) && _elementReferences.TryGetValue(elementId, out var elementRef))
            {
                TooltipService.Open(elementRef, text, new TooltipOptions 
                { 
                    Duration = 0,
                    Style = "background-color: #0e947a !important; background: #0e947a !important; color: white !important; border: none !important; padding: 8px 12px !important; border-radius: 4px !important; font-size: 0.875rem !important;"
                });
            }
        }

        protected void HideTooltip()
        {
            TooltipService.Close();
        }

        #region Image Control Methods

        protected async Task OnImageLoaded(string imageId)
        {
            await JSRuntime.InvokeVoidAsync("eval", $@"
                (function() {{
                    var loadingEl = document.getElementById('loading-{imageId}');
                    var errorEl = document.getElementById('error-{imageId}');
                    var statusEl = document.getElementById('status-{imageId}');
                    var imgEl = document.getElementById('{imageId}');
                    
                    if (loadingEl) loadingEl.style.display = 'none';
                    if (errorEl) errorEl.style.display = 'none';
                    if (imgEl) imgEl.style.display = 'block';
                    if (statusEl) {{
                        statusEl.innerHTML = '<span class=""badge bg-success small""><i class=""fa-solid fa-circle-check me-1""></i>Yüklendi</span>';
                    }}
                }})();
            ");
        }

        protected async Task OnImageError(string imageId, string imageUrl)
        {
            await JSRuntime.InvokeVoidAsync("eval", $@"
                (function() {{
                    var loadingEl = document.getElementById('loading-{imageId}');
                    var errorEl = document.getElementById('error-{imageId}');
                    var statusEl = document.getElementById('status-{imageId}');
                    var imgEl = document.getElementById('{imageId}');
                    
                    if (loadingEl) loadingEl.style.display = 'none';
                    if (errorEl) errorEl.style.display = 'flex';
                    if (imgEl) imgEl.style.display = 'none';
                    if (statusEl) {{
                        statusEl.innerHTML = '<span class=""badge bg-danger small""><i class=""fa-solid fa-triangle-exclamation me-1""></i>Hata</span>';
                    }}
                }})();
            ");
        }

        protected async Task RetryImageLoad(string imageId, string imageUrl)
        {
            await JSRuntime.InvokeVoidAsync("eval", $@"
                (function() {{
                    var loadingEl = document.getElementById('loading-{imageId}');
                    var errorEl = document.getElementById('error-{imageId}');
                    var statusEl = document.getElementById('status-{imageId}');
                    var imgEl = document.getElementById('{imageId}');
                    
                    if (loadingEl) loadingEl.style.display = 'flex';
                    if (errorEl) errorEl.style.display = 'none';
                    if (imgEl) {{
                        imgEl.style.display = 'none';
                        // Force reload by adding timestamp
                        var newUrl = '{imageUrl}' + (/{imageUrl}/.test('{imageUrl}') ? '&' : '?') + '_t=' + new Date().getTime();
                        imgEl.src = newUrl;
                        imgEl.style.display = 'block';
                    }}
                    if (statusEl) {{
                        statusEl.innerHTML = '<span class=""badge bg-warning small""><i class=""fa-solid fa-spinner fa-spin me-1""></i>Yeniden deneniyor...</span>';
                    }}
                }})();
            ");
        }

        protected async Task CheckImageStatus(string imageId, string imageUrl)
        {
            try
            {
                var imageInfo = await JSRuntime.InvokeAsync<ImageInfo>("eval", $@"
                    (function() {{
                        var img = document.getElementById('{imageId}');
                        if (!img) return null;
                        
                        return {{
                            loaded: img.complete && img.naturalHeight !== 0,
                            naturalWidth: img.naturalWidth || 0,
                            naturalHeight: img.naturalHeight || 0,
                            src: img.src || '{imageUrl}',
                            currentSrc: img.currentSrc || img.src || '{imageUrl}'
                        }};
                    }})();
                ");

                if (imageInfo != null)
                {
                    var statusText = imageInfo.Loaded ? "✓ Yüklendi" : "✗ Yüklenemedi";
                    var detailMessage = $"Durum: {statusText}\n";
                    detailMessage += $"Boyut: {imageInfo.NaturalWidth} x {imageInfo.NaturalHeight} px\n";
                    detailMessage += $"URL: {imageInfo.Src}";

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = imageInfo.Loaded ? NotificationSeverity.Success : NotificationSeverity.Warning,
                        Summary = "Görsel Kontrol",
                        Detail = detailMessage,
                        Duration = 5000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Görsel Kontrol",
                        Detail = "Görsel elementi bulunamadı.",
                        Duration = 3000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = $"Görsel kontrol edilirken hata oluştu: {ex.Message}",
                    Duration = 4000
                });
            }
        }

        private class ImageInfo
        {
            public bool Loaded { get; set; }
            public int NaturalWidth { get; set; }
            public int NaturalHeight { get; set; }
            public string Src { get; set; } = "";
            public string CurrentSrc { get; set; } = "";
        }

        #endregion

        #region ML Search Analytics

        private DateTime _searchStartTime = DateTime.UtcNow;
        
        #endregion

        #region VIN OEM Search

        /// <summary>
        /// OEM kodları listesi ile direkt seller_index'te arama yapar
        /// VIN decode sonrası kullanılır - tüm OEM kodlarını kullanır
        /// PERFORMANS: PageSize=50, Similar count skip, Aggregation skip
        /// </summary>
        protected async Task SearchProductsByOemCodes(List<string> oemCodes)
        {
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            Logger.LogInformation("[PERF-VIN] VIN araması başladı - OEM kod sayısı: {Count}", oemCodes.Count);
            
            if (!oemCodes.Any())
            {
                Logger.LogError("[PERF-VIN] HATA: OEM kodları boş!");
                NotificationService.Notify(new NotificationMessage 
                { 
                    Severity = NotificationSeverity.Error, 
                    Summary = "Hata", 
                    Detail = "OEM kodları boş. Lütfen tekrar deneyin." 
                });
                isLoading = false;
                await InvokeAsync(StateHasChanged);
                return;
            }
            
            isLoading = true;
            _searchStartTime = DateTime.UtcNow;
            await InvokeAsync(StateHasChanged);
            
            hasSearched = true;
            try
            {
                // Update state for subsequent filter toggles
                selectedOemCodes = oemCodes;
                selectedDatProcessNumbers.Clear(); // OEM is fallback or specific choice, clear DPN
                
                var filterStopwatch = System.Diagnostics.Stopwatch.StartNew();
                // Tek sorguda tüm OEM kodları ile ara - PageSize=50 (hızlı yükleme)
                var filter = new SearchFilterReguestDto
                {
                    OemCodes = selectedOemCodes, // Direkt OEM kodları - Elasticsearch terms query kullanacak
                    OnlyInStock = showOnlyInStock,
                    OnlyWithImage = showOnlyWithImage,
                    CategoryIds = selectedCategoryIds.Any() ? selectedCategoryIds : null,
                    BrandIds = selectedBrandIds.Any() ? selectedBrandIds : null,
                    ProductIds = selectedProductIds.Any() ? selectedProductIds : null,
                    DotPartNames = selectedDotPartNames.Any() ? selectedDotPartNames.ToList() : null,
                    DatProcessNumbers = selectedDatProcessNumbers.Any() ? selectedDatProcessNumbers : null,
                    ManufacturerNames = !string.IsNullOrEmpty(selectedManufacturerName) ? new List<string> { selectedManufacturerName } : null,
                    BaseModelNames = !string.IsNullOrEmpty(selectedBaseModelName) ? new List<string> { selectedBaseModelName } : null,
                    ManufacturerKey = selectedManufacturerKey,
                    BaseModelKey = selectedBaseModelKey,
                    SingleManufacturerName = selectedManufacturerName,
                    SingleModelName = selectedBaseModelName,
                    SingleSubModelName = selectedSubModelName,
                    SubModelKeys = string.IsNullOrEmpty(selectedSubModelKey) ? null : new List<string> { selectedSubModelKey },
                    Page = 1,
                    PageSize = 50, // PERFORMANS: İlk 50 ürün - hızlı yükleme
                    ShouldGroupOems = !isIncludeEquivalents
                };
                filterStopwatch.Stop();
                Logger.LogInformation("[PERF-VIN] Filter oluşturma: {Duration}ms", filterStopwatch.ElapsedMilliseconds);

                var searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await ProductSearchService.GetByFilterPagingAsync(filter);
                
                // KADEMELİ FALLBACK MANTIĞI:
                // 1. Kademe: Model filtresini kaldır, Markayı koru (AYAR BAĞIMSIZ - Aynı markada parça eşleşmesi güvenlidir)
                if ((!result.Ok || result.Result?.DataCount == 0) && !string.IsNullOrEmpty(filter.BaseModelKey))
                {
                    Logger.LogInformation("[PERF-VIN] Orijinal modelde bulunamadı, marka bazlı eşdeğerlere bakılıyor...");
                    filter.BaseModelNames = null;
                    filter.BaseModelKey = null;
                    filter.SingleModelName = null;
                    filter.SubModelKeys = null;
                    filter.SingleSubModelName = null;
                    
                    result = await ProductSearchService.GetByFilterPagingAsync(filter);
                }

                // 2. Kademe: Markayı da kaldır (AYARA BAĞLI - Cross-brand muadillere bak)
                var allowCrossBrand = Configuration.GetValue<bool>("AppSettings:OriginalUrunBulunamayincaEsdegerBaksinMi");
                if ((!result.Ok || result.Result?.DataCount == 0) && allowCrossBrand)
                {
                    Logger.LogInformation("[PERF-VIN] Marka bazlı aramada da bulunamadı, genel eşdeğerlere bakılıyor...");
                    filter.ManufacturerNames = null;
                    filter.BaseModelNames = null;
                    filter.ManufacturerKey = null;
                    filter.BaseModelKey = null;
                    filter.SingleManufacturerName = null;
                    filter.SingleModelName = null;
                    filter.PageSize = 50;
                    
                    result = await ProductSearchService.GetByFilterPagingAsync(filter);
                    Logger.LogInformation("[PERF-VIN] Eşdeğer araması sonucu: {Ok}, DataCount: {DataCount}", result.Ok, result.Result?.DataCount ?? 0);
                }

                searchStopwatch.Stop();
                Logger.LogInformation("[PERF-VIN] Elasticsearch + JoinRelatedData: {Duration}ms, Ok: {Ok}, DataCount: {DataCount}", 
                    searchStopwatch.ElapsedMilliseconds, result.Ok, result.Result?.DataCount ?? 0);
                
                if (result.Ok && result.Result != null && result.Result.Data != null)
                {
                    var processingStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    searchResult = result.Result;
                    
                    // Sıralama: Stokta olanlar önce, sonra fiyata göre
                    if (searchResult.Data != null && searchResult.Data.Any())
                    {
                        searchResult.Data = searchResult.Data
                            .OrderByDescending(x => x.Stock > 0)
                            .ThenBy(x => x.SalePrice)
                            .ToList();
                    }
                    processingStopwatch.Stop();
                    Logger.LogInformation("[PERF-VIN] Sonuç işleme: {Duration}ms, Ürün: {Count}", 
                        processingStopwatch.ElapsedMilliseconds, searchResult.Data?.Count ?? 0);
                }
                else
                {
                    Logger.LogWarning("[PERF-VIN] Sonuç bulunamadı - Ok: {Ok}, Result: {Result}, Data: {Data}", 
                        result.Ok, result.Result != null, result.Result?.Data != null);
                    
                    searchResult = new Paging<List<SellerProductViewModel>> 
                    { 
                        Data = new List<SellerProductViewModel>(), 
                        DataCount = 0 
                    };
                }

                // PERFORMANS: VIN aramasında aggregation skip - 500ms kazanç
                manufacturerNames.Clear();
                baseModelNames.Clear();
                subModelNames.Clear();

                ExtractFilters();

                isLoading = false;
                await InvokeAsync(StateHasChanged);
                
                // Force grouping update after grid render
                if (isGroupedBySeller && searchGrid != null)
                {
                    await Task.Delay(50);
                    await UpdateGridGroups();
                }
                
                totalStopwatch.Stop();
                Logger.LogInformation("[PERF-VIN] *** TOPLAM: {Duration}ms *** (Hedef: 3000ms)", 
                    totalStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PERF-VIN] HATA");
                hasSearched = true; 
                searchResult = new Paging<List<SellerProductViewModel>> { Data = new List<SellerProductViewModel>(), DataCount = 0 };
                
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = ex.Message });
            }
            finally
            {
                if (isLoading)
                {
                    isLoading = false;
                }
                
                await InvokeAsync(StateHasChanged);
                await Task.Delay(50);
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// VIN decode sonrası DatProcessNumber listesi ile ürün araması yapar
        /// PRIMARY arama yöntemi - OEM kodlarından daha güvenilir
        /// PERFORMANS: PageSize=50, Similar count skip, Aggregation skip
        /// </summary>
        protected async Task SearchProductsByDatProcessNumbers(List<string> datProcessNumbers)
        {
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            Logger.LogInformation("[PERF-DPN] DatProcessNumber araması başladı - Kod sayısı: {Count}", datProcessNumbers.Count);
            
            if (!datProcessNumbers.Any())
            {
                Logger.LogError("[PERF-DPN] HATA: DatProcessNumbers boş!");
                NotificationService.Notify(new NotificationMessage 
                { 
                    Severity = NotificationSeverity.Error, 
                    Summary = "Hata", 
                    Detail = "Parça kodları boş. Lütfen tekrar deneyin." 
                });
                isLoading = false;
                await InvokeAsync(StateHasChanged);
                return;
            }
            
            isLoading = true;
            _searchStartTime = DateTime.UtcNow;
            await InvokeAsync(StateHasChanged);
            
            hasSearched = true;
            try
            {
                // Update state for subsequent filter toggles
                selectedDatProcessNumbers = datProcessNumbers;
                selectedOemCodes.Clear(); // DPN is primary, clear OEM fallback
                
                var filterStopwatch = System.Diagnostics.Stopwatch.StartNew();
                // Tek sorguda tüm DatProcessNumbers ile ara - PageSize=50 (hızlı yükleme)
                var filter = new SearchFilterReguestDto
                {
                    DatProcessNumbers = selectedDatProcessNumbers, // PRIMARY: DatProcessNumber ile arama
                    OemCodes = null, // OEM kodları kullanma - DatProcessNumber öncelikli
                    OnlyInStock = showOnlyInStock,
                    OnlyWithImage = showOnlyWithImage,
                    CategoryIds = selectedCategoryIds.Any() ? selectedCategoryIds : null,
                    BrandIds = selectedBrandIds.Any() ? selectedBrandIds : null,
                    ProductIds = selectedProductIds.Any() ? selectedProductIds : null,
                    DotPartNames = selectedDotPartNames.Any() ? selectedDotPartNames.ToList() : null,
                    
                    // Şasi aramasında marka ve model kısıtlamalarını geri alalım (Alakasız sonuçları önlemek için)
                    ManufacturerNames = !string.IsNullOrEmpty(selectedManufacturerName) ? new List<string> { selectedManufacturerName } : null,
                    BaseModelNames = !string.IsNullOrEmpty(selectedBaseModelName) ? new List<string> { selectedBaseModelName } : null,
                    ManufacturerKey = selectedManufacturerKey,
                    BaseModelKey = selectedBaseModelKey,
                    SingleManufacturerName = selectedManufacturerName,
                    SingleModelName = selectedBaseModelName,
                    
                    SubModelNames = null,
                    SingleSubModelName = null,
                    SubModelKeys = null,

                    MinPrice = minPrice.HasValue && minPrice.Value > 0 ? minPrice : (minPrice == null ? 0.01 : minPrice), 
                    MaxPrice = maxPrice,
                    Page = 1,
                    PageSize = 50, // PERFORMANS: İlk 50 ürün - hızlı yükleme
                    ShouldGroupOems = !isIncludeEquivalents
                };
                filterStopwatch.Stop();
                Logger.LogInformation("[PERF-DPN] Filter oluşturma: {Duration}ms", filterStopwatch.ElapsedMilliseconds);

                var searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await ProductSearchService.GetByFilterPagingAsync(filter);
                
                // KADEMELİ FALLBACK MANTIĞI:
                // 1. Kademe: Model filtresini kaldır, Markayı koru (AYAR BAĞIMSIZ - Aynı markada parça eşleşmesi güvenlidir)
                if ((!result.Ok || result.Result?.DataCount == 0) && !string.IsNullOrEmpty(filter.BaseModelKey))
                {
                    Logger.LogInformation("[PERF-DPN] Orijinal modelde bulunamadı, marka bazlı eşdeğerlere bakılıyor...");
                    filter.BaseModelNames = null;
                    filter.BaseModelKey = null;
                    filter.SingleModelName = null;
                    filter.SubModelNames = null;
                    filter.SingleSubModelName = null;
                    
                    result = await ProductSearchService.GetByFilterPagingAsync(filter);
                }

                // 2. Kademe: Markayı da kaldır (AYARA BAĞLI - Cross-brand muadillere bak)
                var allowCrossBrand = Configuration.GetValue<bool>("AppSettings:OriginalUrunBulunamayincaEsdegerBaksinMi");
                if ((!result.Ok || result.Result?.DataCount == 0) && allowCrossBrand)
                {
                    Logger.LogInformation("[PERF-DPN] Marka bazlı aramada da bulunamadı, genel eşdeğerlere bakılıyor...");
                    filter.ManufacturerNames = null;
                    filter.BaseModelNames = null;
                    filter.ManufacturerKey = null;
                    filter.BaseModelKey = null;
                    filter.SingleManufacturerName = null;
                    filter.SingleModelName = null;
                    filter.PageSize = 50;
                    
                    result = await ProductSearchService.GetByFilterPagingAsync(filter);
                    Logger.LogInformation("[PERF-DPN] Eşdeğer araması sonucu: {Ok}, DataCount: {DataCount}", result.Ok, result.Result?.DataCount ?? 0);
                }

                searchStopwatch.Stop();
                Logger.LogInformation("[PERF-DPN] Elasticsearch + JoinRelatedData: {Duration}ms, Ok: {Ok}, DataCount: {DataCount}", 
                    searchStopwatch.ElapsedMilliseconds, result.Ok, result.Result?.DataCount ?? 0);
                
                if (result.Ok && result.Result != null && result.Result.Data != null)
                {
                    var processingStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    searchResult = result.Result;
                    
                    // Sıralama: Stokta olanlar önce, sonra fiyata göre
                    if (searchResult.Data != null && searchResult.Data.Any())
                    {
                        searchResult.Data = searchResult.Data
                            .OrderByDescending(x => x.Stock > 0)
                            .ThenBy(x => x.SalePrice)
                            .ToList();
                    }
                    processingStopwatch.Stop();
                    Logger.LogInformation("[PERF-DPN] Sonuç işleme: {Duration}ms, Ürün: {Count}", 
                        processingStopwatch.ElapsedMilliseconds, searchResult.Data?.Count ?? 0);
                }
                else
                {
                    Logger.LogWarning("[PERF-DPN] Sonuç bulunamadı - Ok: {Ok}, Result: {Result}, Data: {Data}", 
                        result.Ok, result.Result != null, result.Result?.Data != null);
                    
                    searchResult = new Paging<List<SellerProductViewModel>> 
                    { 
                        Data = new List<SellerProductViewModel>(), 
                        DataCount = 0 
                    };
                }

                // PERFORMANS: VIN aramasında aggregation skip - 500ms kazanç
                manufacturerNames.Clear();
                baseModelNames.Clear();
                subModelNames.Clear();

                ExtractFilters();

                isLoading = false;
                await InvokeAsync(StateHasChanged);
                
                // Force grouping update after grid render
                if (isGroupedBySeller && searchGrid != null)
                {
                    await Task.Delay(50);
                    await UpdateGridGroups();
                }
                
                totalStopwatch.Stop();
                Logger.LogInformation("[PERF-DPN] *** TOPLAM: {Duration}ms *** (Hedef: 3000ms)", 
                    totalStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PERF-DPN] HATA");
                hasSearched = true; 
                searchResult = new Paging<List<SellerProductViewModel>> { Data = new List<SellerProductViewModel>(), DataCount = 0 };
                
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = ex.Message });
            }
            finally
            {
                if (isLoading)
                {
                    isLoading = false;
                }
                
                await InvokeAsync(StateHasChanged);
                await Task.Delay(50);
                await InvokeAsync(StateHasChanged);
            }
        }

        #endregion
    }
}
