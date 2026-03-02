using Blazored.LocalStorage;
using ecommerce.Core.Dtos;
using ecommerce.Core.Entities;
using ecommerce.Domain.Shared.Dtos.Favorite;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Utility;
using ecommerce.Web.Events;
using System.Threading;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Web.Components.Pages;

public partial class WishListPage : IDisposable
{
    private CancellationTokenSource? _renderCts;
    private bool _isDisposed = false;
    [Inject] private II18N lang { get; set; }
    [Inject] private ILocalStorageService _localStorage { get; set; }
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    [Inject] private IFavoriteService FavoriteService { get; set; }
    private List<ProductFavoriteDto> FavoriteProducts { get; set; } = null;
    [Inject] private CdnOptions CdnConfig { get; set; }
    [Inject] private NotificationService _notificationService { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private IUserManager _userManager { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    [Inject] private ICartService _cartService { get; set; }
    private Core.Entities.Authentication.User User { get; set; } = new();
    private int CurrentPage => page;
    private int TotalPages => (int)Math.Ceiling((double)totalCount / pageSize);

    private UserClaims _userClaims = new();
    private int page = 1;
    private int pageSize = 20;
    private int totalCount = 0;
    private CartDto CartResult { get; set; } = new();
    private bool favoritesLoaded;
    
    private bool _shouldRender = true;
    protected override bool ShouldRender() => _shouldRender;

    protected override async Task OnInitializedAsync()
    {
        _appStateManager.StateChanged += AppState_StateChanged;
        CartResult = await _appStateManager.GetCart();
        await LoadFavorites();
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
                Console.WriteLine($"⚠️ WishListPage.AppState_StateChanged error: {ex.Message}");
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
      
            try
            {
                // feather.replace removed to prevent DOM conflicts
            }
            catch { }

            try
            {
                var localLanguage = await _localStorage.GetItemAsync<string>("lang");
                if (localLanguage != null)
                {
                    _appStateManager.InvokeLanguageChanged(localLanguage);
                    lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
                }
            }
            catch (Exception ex)
            {
                // Prerendering sırasında localStorage erişimi hata verebilir, ignore edelim
                Console.WriteLine($"LocalStorage access error: {ex.Message}");
            }

            try
            {
                await _jsRuntime.InvokeVoidAsync("ensureWishlistZ");
            }
            catch { }
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _appStateManager.StateChanged -= AppState_StateChanged;
        GC.SuppressFinalize(this);
    }

    private async Task OnPageChanged(int newPage)
    {
        page = newPage;
        await LoadFavorites();
        await _jsRuntime.InvokeVoidAsync("window.scrollTo", 0, 0);
    }

    private async Task LoadFavorites(bool showGlobalLoading = true)
    {
        favoritesLoaded = false;
        _shouldRender = true;
                await RequestRender();
        Func<Task> loader = async () =>
        {
            try
            {
                var result = await FavoriteService.GetAllFavoritesAsync(page, pageSize);
                if (result.Ok)
                {
                    FavoriteProducts = result.Result?.Data ?? new List<ProductFavoriteDto>();
                    totalCount = result.Result?.DataCount ?? 0;
                }
                else
                {
                    FavoriteProducts = new List<ProductFavoriteDto>();
                }
            }
            catch (Exception ex)
            {
                FavoriteProducts = new List<ProductFavoriteDto>();
                Console.WriteLine($"Favoriler yüklenirken hata: {ex.Message}");
            }
            finally
            {
                favoritesLoaded = true;
                _shouldRender = true;
                await RequestRender();
            }
        };

        if (showGlobalLoading)
        {
            await _appStateManager.ExecuteWithLoading(loader, "Favoriler yükleniyor");
        }
        else
        {
            await loader();
        }
    }

    private async Task RemoveFromFavorite(int productId)
    {
        await _appStateManager.ExecuteWithLoading(async () => {
            var result = await FavoriteService.DeleteFavoriteForCurrentUserAsync(productId);
            if (!result.Ok)
            {
                _notificationService.Notify(NotificationSeverity.Error, lang["FavoriteRemoveFailed"]);
            }
            else
            {
                _notificationService.Notify(NotificationSeverity.Success, lang["FavoriteRemoved"]);
                await LoadFavorites(false);
            }
        }, "Favori kaldırılıyor");
    }

    private async Task CartInsert(CartItemUpsertDto cartItem, bool isAdd)
    {
        await _appStateManager.ExecuteWithLoading(async () => {
            try
            {
                var result = await _cartService.CreateCartItem(cartItem);
                if (result.Ok)
                {
                    var cartResult = await _cartService.GetCart();
                    if (cartResult.Ok)
                    {
                        CartResult = cartResult.Result;
                        _shouldRender = true;
                        await RequestRender();
                    }
                    _appStateManager.UpdatedCart(this, cartResult?.Result);
                    _notificationService.Notify(NotificationSeverity.Success, isAdd ? lang["AddedToCart"] : lang["UpdatedCart"]);
                }
                else
                {
                    _notificationService.Notify(NotificationSeverity.Error, lang["CartError"]);
                }
            }
            catch (Exception ex)
            {
                _notificationService.Notify(NotificationSeverity.Error, lang["CartError"]);
                Console.WriteLine($"Cart insert error: {ex.Message}");
            }
        }, "Sepete ekleniyor");
    }

}