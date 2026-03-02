using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Modal.Services;
using ecommerce.Core.Dtos;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Domain.Shared.Dtos.Favorite;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.Web.Components.Modals;
using ecommerce.Web.Domain.Dtos.Order;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using ecommerce.Web.Events;
using System.Threading;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Core.Utils;
namespace ecommerce.Web.Components.Pages;
public partial class UserDashboardPage : IDisposable {
    [CascadingParameter] public IModalService _openModal{get;set;}
    [Inject] private II18N lang{get;set;}
    [Inject] private ILocalStorageService _localStorage{get;set;}
    [Inject] private IJSRuntime _jsRuntime{get;set;}
    [Inject] private NotificationService _notificationService{get;set;}
    [Inject] private AppStateManager _appStateManager{get;set;}
    [Inject] private ICommonManager _commonManager{get;set;}
    [Inject] private IUserManager _userManager{get;set;}
    [Inject] private DialogService _dialogService{get;set;}
    [Inject] private NavigationManager _navigationManager{get;set;}
    [Inject] private IFavoriteService FavoriteService{get;set;}
    [Inject] private IUserCarService _userCarService{get;set;}
    [Inject] private IDotIntegrationService _dotService{get;set;}
    [Inject] private IServiceScopeFactory _serviceScopeFactory { get; set; }
    [Inject] private IUserOrderService _userOrderService { get; set; }
    private List<ProductFavoriteDto> FavoriteProducts{get;set;} = new();
    [Inject] private CdnOptions CdnConfig{get;set;}
    private int CurrentPage => page;
    private int TotalPages => (int) Math.Ceiling((double) totalCount / pageSize);
    private int page = 1;
    private int pageSize = 20;
    private int totalCount = 0;
    private ecommerce.Web.Domain.Dtos.Cart.CartDto CartResult{get;set;} = new();
    private CancellationTokenSource? _renderCts;
    private bool _isDisposed = false;

    private User User{get;set;} = new();
    private List<UserAddress> AddressList{get;set;} = new();
    private string CurrentPassword{get;set;}
    private string NewPassword{get;set;}
    private string ConfirmPassword{get;set;}
    private bool IsFavoriteTabActive{get;set;} = false;
    private int TotalFavoritesCount{get;set;} = 0;
    private int TotalOrdersCount{get;set;} = 0;
    private int PendingOrdersCount{get;set;} = 0;
    private List<UserCars> UserCarList{get;set;} = new();
    private UserOrderHistoryDto OrderHistory { get; set; } = new();
    private List<OrderDto> FilteredOrders { get; set; } = new();
    private List<OrderDto> DisplayedOrders { get; set; } = new(); 
    private int OrdersPerPage { get; set; } = 5; 
    private int CurrentOrderPage { get; set; } = 1; 
    private bool IsLoadingMoreOrders { get; set; } = false; 
   
    // Accordion state tracking
    private HashSet<int> ExpandedCargoIds { get; set; } = new();
    private HashSet<int> ExpandedPriceIds { get; set; } = new();
    private HashSet<int> ExpandedBankIds { get; set; } = new();
   
    private bool IsLoadingOrders { get; set; } = false;
   
    private Dictionary<int, string?> CarPreviewImages { get; set; } = new();
    private bool IsModalOpen { get; set; } = false;
    protected override async Task OnInitializedAsync(){
        _appStateManager.StateChanged += AppState_StateChanged;
        CartResult = await _appStateManager.GetCart();

        var uri = new Uri(_navigationManager.Uri);
        var fragment = uri.Fragment;
        if(!string.IsNullOrEmpty(fragment) && fragment.Trim().Equals("#pills-wishlist", StringComparison.OrdinalIgnoreCase)){
            IsFavoriteTabActive = true;
        }
        
        var userResult = await _userManager.GetCurrentUserAsync();
        if(userResult.Ok && userResult.Result != null){
            User = userResult.Result;
        }
        await LoadFavorites();
        await LoadAddresses();
        await LoadOrders();
        await LoadDashboardStats(); // Load statistics for dashboard cards
        await LoadCars();
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
                Console.WriteLine($"⚠️ UserDashboardPage.AppState_StateChanged error: {ex.Message}");
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

    private OrderStatusType CurrentStatus { get; set; } = OrderStatusType.OrderNew;

    private async Task LoadOrders(){
        try{
            IsLoadingOrders = true;
            CurrentOrderPage = 1;
            await RequestRender();
            
            // İlk 5 siparişi yükle (status ile)
            OrderHistory = await _userOrderService.GetUserOrderHistoryAsync(
                CurrentStatus, 
                CurrentOrderPage, 
                OrdersPerPage);
            Console.WriteLine($"📦 Initial load: {OrderHistory.GetAllOrders().Count} orders, Total: {OrderHistory.TotalCount}, Status: {CurrentStatus}");
            
            // Client-side filtering artık gerekmiyor çünkü server-side yapılıyor
            DisplayedOrders = OrderHistory.GetAllOrders().ToList();
            
            // Re-init scroll observer if needed (after render)
            // Note: FilterOrdersByStatus is not called here anymore to avoid recursion/double load
        } catch(Exception ex){
            Console.WriteLine($"Error loading orders: {ex.Message}");
        } finally {
            IsLoadingOrders = false;
            await RequestRender();
        }
    }

    private async Task FilterOrdersByStatus(OrderStatusType? status)
    {
        CurrentStatus = status ?? OrderStatusType.OrderNew;
        
        // Server'dan yeni data çek
        await LoadOrders();
        
        // UI'ın güncellenmesi için kısa bir bekleme
        await Task.Delay(100);
        
        // Re-init infinite scroll
        try {
            if (dotNetHelper != null)
                await _jsRuntime.InvokeVoidAsync("initInfiniteScroll", dotNetHelper);
        } catch { /* ignore */ }
    }

    private async Task LoadMoreOrders()
    {
        Console.WriteLine($"🔄 LoadMoreOrders called");
        
        if (IsLoadingMoreOrders)
        {
            Console.WriteLine($"⚠️ Already loading, skipping");
            return;
        }
        
        // Toplam sayıyı kontrol et
        var totalOrders = OrderHistory.TotalCount;
        var currentlyLoaded = OrderHistory.GetAllOrders().Count;
        
        Console.WriteLine($"📊 Total: {totalOrders}, Currently Loaded: {currentlyLoaded}");
        
        if (currentlyLoaded >= totalOrders)
        {
            Console.WriteLine($"✅ All orders loaded");
            return; // Tümü yüklü
        }
        
        IsLoadingMoreOrders = true;
        await RequestRender();
        
        try
        {
            CurrentOrderPage++;
            Console.WriteLine($"📄 Loading page {CurrentOrderPage} for status: {CurrentStatus}");
            
            // Server'dan sonraki sayfayı yükle (status ile)
            var moreData = await _userOrderService.GetUserOrderHistoryAsync(
                CurrentStatus,
                CurrentOrderPage,
                OrdersPerPage);
            
            Console.WriteLine($"✅ Received {moreData.GetAllOrders().Count} orders from server");
            
            // Merge data
            foreach (var sellerGroup in moreData.SellerGroups)
            {
                var existingGroup = OrderHistory.SellerGroups.FirstOrDefault(g => g.SellerId == sellerGroup.SellerId);
                if (existingGroup != null)
                {
                    existingGroup.Orders.AddRange(sellerGroup.Orders);
                }
                else
                {
                    OrderHistory.SellerGroups.Add(sellerGroup);
                }
            }
            
            // Update total count
            OrderHistory.TotalCount = moreData.TotalCount;
            
            Console.WriteLine($"📊 After merge: {OrderHistory.GetAllOrders().Count} total orders");
            
            // Update displayed orders
            DisplayedOrders = OrderHistory.GetAllOrders().ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading more orders: {ex.Message}");
        }
        finally
        {
            IsLoadingMoreOrders = false;
            await RequestRender();
        }
    }

    private int GetOrderCountByStatus(OrderStatusType? status)
    {
        if (status == null) return 0;
        if (OrderHistory.StatusCounts.TryGetValue(status.ToString(), out var count))
        {
            return count;
        }
        return 0;
    }

   
    // Accordion Toggle Methods
    private void ToggleCargoSection(int orderId)
    {
        if (ExpandedCargoIds.Contains(orderId))
            ExpandedCargoIds.Remove(orderId);
        else
            ExpandedCargoIds.Add(orderId);
    }
    
    private void TogglePriceSection(int orderId)
    {
        if (ExpandedPriceIds.Contains(orderId))
            ExpandedPriceIds.Remove(orderId);
        else
            ExpandedPriceIds.Add(orderId);
    }
    
    private void ToggleBankSection(int orderId)
    {
        if (ExpandedBankIds.Contains(orderId))
            ExpandedBankIds.Remove(orderId);
        else
            ExpandedBankIds.Add(orderId);
    }
    
    private bool IsCargoExpanded(int orderId) => ExpandedCargoIds.Contains(orderId);
    private bool IsPriceExpanded(int orderId) => ExpandedPriceIds.Contains(orderId);
    private bool IsBankExpanded(int orderId) => ExpandedBankIds.Contains(orderId);
    
    private async Task CancelPayment(OrderDto order)
    {
        // Detailed confirmation with order information
        var itemCount = order.Items?.Count ?? 0;
        var confirmMessage = string.Format(
            lang["Order.Cancel.ConfirmMessage"],
            order.OrderNumber, 
            order.GrandTotal.ToString("F2"), 
            itemCount
        );
        
        var confirm = await _dialogService.Confirm(
            confirmMessage,
            lang["Order.Cancel.Title"],
            new ConfirmOptions { OkButtonText = lang["Order.Cancel.ButtonConfirm"], CancelButtonText = lang["Order.Cancel.ButtonCancel"] });
        
        if (confirm == true)
        {
            try
            {
                var (success, message) = await _userOrderService.CancelOrder(order.Id, string.Empty);
                
                if (success)
                {
                    _notificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = lang["Order.Cancel.Success"],
                        Detail = message,
                        Duration = 4000
                    });
                    
                    // Refresh orders
                    await LoadOrders();
                    await RequestRender();
                }
                else
                {
                    _notificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = lang["Order.Cancel.Failed"],
                        Detail = message,
                        Duration = 4000
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CancelPayment] Error: {ex.Message}");
                _notificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = lang["Order.Cancel.Error"],
                    Detail = lang["Order.Cancel.ErrorMessage"],
                    Duration = 4000
                });
            }
        }
    }
    

    protected override async Task OnAfterRenderAsync(bool firstRender){
        if(firstRender){
            // Handle Payment Callbacks (Moved from OnInitializedAsync to avoid 500 Error)
            var uriInput = new Uri(_navigationManager.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uriInput.Query);
            if(query.TryGetValue("success", out var successVal) && successVal == "true")
            {
                if(query.TryGetValue("orderNumber", out var orderNo))
                {
                     _notificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Ödeme Başarılı",
                        Detail = $"Siparişiniz başarıyla alındı. Sipariş No: {orderNo}",
                        Duration = 6000
                    });
                }
                
                // Auto switch to orders tab
                await _jsRuntime.InvokeVoidAsync("eval", "var tabTriggerEl = document.querySelector('#pills-order-tab'); if(tabTriggerEl) new bootstrap.Tab(tabTriggerEl).show();");
            }
           
        
        
            await _jsRuntime.InvokeVoidAsync("sliderThree");
            dotNetHelper = DotNetObjectReference.Create(this);
            try
            {
                await _jsRuntime.InvokeVoidAsync("initInfiniteScroll", dotNetHelper);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing infinite scroll: {ex.Message}");
            }
            await RequestRender();
            var localLanguage = await _localStorage.GetItemAsync<string>("lang");
            if(localLanguage != null){
                _appStateManager.InvokeLanguageChanged(localLanguage);
                lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
            }
            var uri = new Uri(_navigationManager.Uri);
            var fragment = uri.Fragment;
            if(!IsFavoriteTabActive && !string.IsNullOrEmpty(fragment)){
                var targetTab = fragment.TrimStart('#');
                await _jsRuntime.InvokeVoidAsync("eval", $"var tabTriggerEl = document.querySelector('[data-bs-target=\"#{targetTab}\"]'); if(tabTriggerEl) new bootstrap.Tab(tabTriggerEl).show();");
            }
            await RequestRender();
        }
    }
    private async Task LoadFavorites(){
        try{
            var result = await FavoriteService.GetAllFavoritesAsync(page, pageSize);
            if(result.Ok){
                FavoriteProducts = result.Result.Data;
                totalCount = result.Result.DataCount;
                TotalFavoritesCount = result.Result.DataCount;
                await RequestRender();
            }
        } catch(Exception ex){
            Console.WriteLine(ex);
        }
    }
    private async Task EditModalOpen(UserAddress address){
        try{
            if(address == null) return;
            var parameters = new ModalParameters();
            parameters.Add(nameof(AddOrUpdateAdressModal.EditableAddress), address);
            var options = new ModalOptions{
                DisableBackgroundCancel = false,
                HideHeader = true,
                Size = ModalSize.Large,
                HideCloseButton = true,
                AnimationType = ModalAnimationType.FadeInOut
            };
            var modalRef = _openModal.Show<AddOrUpdateAdressModal>(@lang["EditAddress"], parameters, options);
            var result = await modalRef.Result;
            if(!result.Cancelled){
                await LoadAddresses();
            }
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
    }
    private async Task LoadAddresses(){
        try{
            var result = await _userManager.GetAllUserAddressesAsync();
            if(result.Result != null){
                AddressList = result.Result
                    .OrderByDescending(a => a.CreatedDate)
                    .ToList();
            }
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        } 
    }
    private async Task DeleteAddress(int addressId){
        try{
            bool ? confirmed = await _dialogService.Confirm(lang["ConfirmDeleteMessage"], lang["DeleteConfirmationTitle"], new ConfirmOptions(){OkButtonText = lang["Yes"], CancelButtonText = lang["No"]});
            if(confirmed != true) return;
            
            await _appStateManager.ExecuteWithLoading(async () => {
                try{
                    var result = await _userManager.DeleteUserAddressAsync(addressId);
                    if(!result.Ok){
                        await LoadAddresses();
                        _notificationService.Notify(NotificationSeverity.Success, lang["DeletedSuccessfully"]);
                    } else{
                        _notificationService.Notify(NotificationSeverity.Error, lang["DeleteFailed"]);
                    }
                } catch(Exception e){
                    Console.WriteLine(e);
                    _notificationService.Notify(NotificationSeverity.Error, lang["DeleteFailed"]);
                }
            }, "Adres siliniyor");
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
    }
    
    private async Task SetDefaultAddress(int addressId){
        try{
            await _appStateManager.ExecuteWithLoading(async () => {
                try{
                    var result = await _userManager.SetDefaultAddressAsync(addressId);
                    if(result.Ok){
                        await LoadAddresses();
                        _notificationService.Notify(NotificationSeverity.Success, "Varsayılan adres ayarlandı");
                    } else{
                        _notificationService.Notify(NotificationSeverity.Error, "Varsayılan adres ayarlanamadı");
                    }
                } catch(Exception e){
                    Console.WriteLine(e);
                    _notificationService.Notify(NotificationSeverity.Error, "Bir hata oluştu");
                }
            }, "Adres ayarlanıyor");
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
    }
    private async Task SaveProfile(){
        await _appStateManager.ExecuteWithLoading(async () => {
            try{
                var result = await _userManager.UpdateUserProfileAsync(User);
                if(result.Ok){
                    _notificationService.Notify(NotificationSeverity.Success, lang["ProfileSucces"]);
                } else{
                    _notificationService.Notify(NotificationSeverity.Error);
                }
            } catch(Exception e){
                Console.WriteLine(e);
                throw;
            }
        }, "Profil kaydediliyor");
    }
    private async Task ChangePassword(){
        try{
            if(string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword)){
                _notificationService.Notify(NotificationSeverity.Error, lang["FillAllFields"]);
                return;
            }
            if(NewPassword != ConfirmPassword){
                _notificationService.Notify(NotificationSeverity.Error, lang["PasswordsDoNotMatch"]);
                return;
            }
            if(!NewPassword.Any(char.IsUpper)){
                _notificationService.Notify(NotificationSeverity.Error, lang["PasswordMustContainUppercase"]);
                return;
            }
            
            await _appStateManager.ExecuteWithLoading(async () => {
                var result = await _userManager.ChangePasswordAsync(CurrentPassword, NewPassword);
                if(result.Ok){
                    _notificationService.Notify(NotificationSeverity.Success, lang["PasswordChanged"]);
                    CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
                } else{
                    _notificationService.Notify(NotificationSeverity.Error, "Hata");
                }
            }, "Şifre değiştiriliyor");
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
    }
    private async Task OnPageChanged(int newPage){
        page = newPage;
        await LoadFavorites();
    }
    private async Task RemoveFromFavorite(int productId){
        
        var result = await FavoriteService.DeleteFavoriteForCurrentUserAsync(productId);
        if(!result.Ok){
            _notificationService.Notify(NotificationSeverity.Error, lang["FavoriteRemoveFailed"]);
        } else{
            _notificationService.Notify(NotificationSeverity.Success, lang["FavoriteRemoved"]);
            await LoadFavorites();
        }
    }
    private async Task EditCarOpen(UserCars car){
        try{
            if(car == null) return;
            var parameters = new ModalParameters();
            parameters.Add(nameof(AddOrUpdateCarModel.EditableCars), car);
            var options = new ModalOptions{
                DisableBackgroundCancel = false,
                HideHeader = true,
                Size = ModalSize.Large,
                HideCloseButton = true,
                AnimationType = ModalAnimationType.FadeInOut
            };
            IsModalOpen = true; // Set flag to prevent LoadCarImages conflicts
            var modalRef = _openModal.Show<AddOrUpdateCarModel>(@lang["EditCar"], parameters, options);
            var result = await modalRef.Result;
            IsModalOpen = false; // Clear flag after modal closes
            if(!result.Cancelled){
                await LoadCars();
            }
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
    }
    private async Task AddNewCar(){
        try{
          

            if (User?.Id == null || User.Id == 0){
                Console.WriteLine("ERROR: Kullanıcı ID'si bulunamadı!");
                _notificationService.Notify(NotificationSeverity.Error, "Kullanıcı bilgileri yüklenemedi. Lütfen sayfayı yenileyin.");
                return;
            }

            // Yeni araç için boş bir UserCars objesi oluştur
            var newCar = new UserCars { UserId = User.Id };
           

            var parameters = new ModalParameters();
            parameters.Add(nameof(AddOrUpdateCarModel.EditableCars), newCar);
            var options = new ModalOptions{
                DisableBackgroundCancel = false,
                HideHeader = true,
                Size = ModalSize.Large,
                HideCloseButton = true,
                AnimationType = ModalAnimationType.FadeInOut
            };
            IsModalOpen = true; // Set flag to prevent LoadCarImages conflicts
            var modalRef = _openModal.Show<AddOrUpdateCarModel>(@lang["AddNewCar"], parameters, options);
            var result = await modalRef.Result;
            IsModalOpen = false; // Clear flag after modal closes
            if(!result.Cancelled){
                await LoadCars();
            }
        } catch(Exception e){
            Console.WriteLine($"ERROR: AddNewCar - {e.Message}");
            _notificationService.Notify(NotificationSeverity.Error, "Yeni araç eklenirken bir hata oluştu.");
        }
    }
    private async Task DeleteCar(int carId)
{
    try
    {
        bool? confirmed = await _dialogService.Confirm(
            lang["Garage.DeleteConfirmMessage"],
            lang["Garage.DeleteConfirmTitle"],
            new ConfirmOptions { OkButtonText = lang["Yes"], CancelButtonText = lang["No"] }
        );

        if (confirmed != true)
            return;

        var result = await _userCarService.DeleteUserCarAsync(carId);
        if (result.Ok && result.Result != null)
        {
            _notificationService.Notify(NotificationSeverity.Success, lang["Garage.DeleteSuccess"]);
            await LoadCars();
        }
        else
        {
            _notificationService.Notify(NotificationSeverity.Error, lang["Garage.DeleteFailed"]);
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        _notificationService.Notify(NotificationSeverity.Error, lang["Garage.DeleteFailed"]);
    }
}
    private async Task LoadCars(){
        await _appStateManager.ExecuteWithLoading(async () => {
            try{
            
                var result = await _userCarService.GetAllUserCarsForCurrentUserAsync();
                if(result.Ok && result.Result != null){
                    UserCarList = result.Result;
                    await LoadCarImages();
                }
            } catch(Exception e){
                Console.WriteLine(e);
                throw;
            } 
        }, lang["Common.Loading"] ?? "Yükleniyor...");
    }

   

    private async Task LoadCarImages()
    {
        if (UserCarList == null || UserCarList.Count == 0) return;
        if (IsModalOpen) return; // Don't load images while modal is open to prevent DbContext conflicts

        try
        {
            // Process sequentially to avoid DbContext concurrency issues
            foreach (var car in UserCarList)
            {
                try
                {
                    var vehicleTypeId = car.DotVehicleTypeId ?? 0;
                    if (vehicleTypeId == 0)
                    {
                        CarPreviewImages[car.Id] = null;
                        continue;
                    }
                    var vehicleTypeStr = vehicleTypeId.ToString();
                    
                    // Prefer related entity DatKey if included; fallback to stored key fields
                    var manufacturerKey = car.DotManufacturer?.DatKey ?? car.DotManufacturerKey;
                    var baseModelKey    = car.DotBaseModel?.DatKey ?? car.DotBaseModelKey;
                    var subModelKey     = car.DotSubModel?.DatKey ?? car.DotSubModelKey;


                    if (string.IsNullOrWhiteSpace(manufacturerKey) || string.IsNullOrWhiteSpace(baseModelKey) || string.IsNullOrWhiteSpace(subModelKey))
                    {
                        CarPreviewImages[car.Id] = null;
                        continue;
                    }

                    // Use GetVehicleImagesByCodesAsync with separate scope to prevent NpgsqlOperationInProgressException
                    try 
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
                        
                        var byCodes = await dotService.GetVehicleImagesByCodesAsync(vehicleTypeStr, manufacturerKey, baseModelKey, subModelKey);
                        
                        if (byCodes.Ok && byCodes.Result != null && byCodes.Result.Count > 0)
                        {
                            // SADECE URL kullan - base64 performans sorunu yaratıyor
                            var preview = byCodes.Result.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Url))?.Url;
                            CarPreviewImages[car.Id] = preview; // NULL ise placeholder gösterilecek
                            
                            if (string.IsNullOrEmpty(preview))
                            {
                                Console.WriteLine($"⚠️ Car {car.Id}: URL bulunamadı! DotVehicleImages tablosunda Url alanını doldurmanız gerekiyor.");
                            }
                        }
                        else
                        {
                            CarPreviewImages[car.Id] = null;
                            Console.WriteLine($"⚠️ Car {car.Id}: Hiç görsel bulunamadı (DatECode: {car.DotManufacturer?.DatKey}-{car.DotBaseModel?.DatKey}-{car.DotSubModel?.DatKey})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR: Car {car.Id} - Image loading failed: {ex.Message}");
                        CarPreviewImages[car.Id] = null;
                    }
                }
                catch (Exception ex)
                {
                    CarPreviewImages[car.Id] = null;
                }
            }

            await RequestRender();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: LoadCarImages - {ex.Message}");
        }
    }
    private string GetStatusClass(string status) => _appStateManager.GetOrderStatusClass(status);

    private string GetStatusIcon(string status) => _appStateManager.GetOrderStatusIcon(status);

    private string GetStatusText(string status) => _appStateManager.GetOrderStatusText(status);

    private string GetProductImageUrl(string fileName, string? fileGuid = null) 
        => _appStateManager.GetProductImageUrl(fileName, fileGuid);

    // Infinite Scroll JS Interop
    private DotNetObjectReference<UserDashboardPage>? dotNetHelper;

   

    [JSInvokable]
    public async Task OnScrollTrigger()
    {
        Console.WriteLine($"🎯 OnScrollTrigger called from JS");
        await LoadMoreOrders();
    }
    
    private async Task LoadDashboardStats()
    {
        try
        {
            // Get total orders (all statuses)
            var allOrdersHistory = await _userOrderService.GetUserOrderHistoryAsync(null, 1, 1);
            TotalOrdersCount = allOrdersHistory.TotalCount;
            
            // Get pending orders (OrderNew + OrderWaitingPayment)
            var newOrders = await _userOrderService.GetUserOrderHistoryAsync(OrderStatusType.OrderNew, 1, 1);
            var waitingOrders = await _userOrderService.GetUserOrderHistoryAsync(OrderStatusType.OrderWaitingPayment, 1, 1);
            PendingOrdersCount = newOrders.TotalCount + waitingOrders.TotalCount;
            
            Console.WriteLine($"📊 Dashboard Stats: Total={TotalOrdersCount}, Pending={PendingOrdersCount}, Favorites={TotalFavoritesCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading dashboard stats: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _appStateManager.StateChanged -= AppState_StateChanged;
        dotNetHelper?.Dispose();
    }
}