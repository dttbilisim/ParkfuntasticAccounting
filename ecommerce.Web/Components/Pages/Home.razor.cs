using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Modal.Services;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Dtos.Banners;
using ecommerce.Domain.Shared.Dtos.Brand;
using ecommerce.Domain.Shared.Dtos.Category;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Components.Modals;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using ecommerce.Web.Events;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using System.Threading;
using Microsoft.JSInterop;
using Radzen;
using Telecom.Address.Abstract;
namespace ecommerce.Web.Components.Pages;

public partial class Home : IDisposable
{
    private ecommerce.Web.Domain.Dtos.Cart.CartDto CartResult = new();
    private CancellationTokenSource? _renderCts;
    private bool _isDisposed = false;
     [CascadingParameter] public IModalService _openModal { get; set; }
    [Inject] private ICategoryService CategoryService { get; set; }
    [Inject] private ISellerProductService _productService { get; set; }
    [Inject] private IBannerService BannerService { get; set; }
    [Inject] private ILocalStorageService _localStorageService { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private IJSRuntime _jsruntime { get; set; }
    [Inject] private CdnOptions CdnConfig { get; set; }


    [Inject] private IBrandService _brandService { get; set; }
    [Inject] private IFavoriteService FavoriteService { get; set; }
[Inject] private NotificationService _notificationService { get; set; }

    private List<CategoryElasticDto> CategoryList = [];
    private List<BannerItemDto> BannerItemList = [];
    private List<SellerProductViewModel> ProductList = new();
    private List<BrandElasticDto> BrandList = new();
    private int currentUserId = 0;
    

    protected override async Task OnInitializedAsync(){
        _appStateManager.StateChanged += AppState_StateChanged;
        CartResult = await _appStateManager.GetCart();
        
        await _appStateManager.ExecuteWithLoading(async () => {
            // OPTIMIZE: Paralel data loading - 4x hızlı!
            await Task.WhenAll(
                GetCategoryList(),
                GetBannerList(),
                GetProductList(),
                GetBrandList()
            );
        }, "Ana sayfa yükleniyor");
    }

    private async Task GetCategoryList()
    {
        var result = await CategoryService.GetAllWithIsMainPageAsync();
        if (result.Ok && result.Result != null)
        {
            CategoryList = result.Result;
            await RequestRender();
        }
    }

    private async Task GetProductList()
    {
        var result = await _productService.GetAllAsync();
        if (result.Ok && result.Result != null)
        {
            ProductList = result.Result;
            await RequestRender();
        }
    }

    private async Task GetBrandList()
    {
        var result = await _brandService.GetAllAsync();
        if (result.Ok && result.Result != null)
        {
            BrandList = result.Result
                .OrderByDescending(x => !string.IsNullOrEmpty(x.ImageUrl))
                .ThenBy(x => x.Name)
                .ToList();
            await RequestRender();
        }
    }

    private async Task GetBannerList()
    {
        var result = await BannerService.GetAllAsync(BannerType.MainPageHeader);
        if (result.Ok && result.Result != null)
        {
            BannerItemList = result.Result;
            await RequestRender();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // OPTIMIZE: sliderThree kaldırıldı - SlickSlider component'i direkt initialize ediyor
                // Banner slider artık daha hızlı yükleniyor!
             
                if (_localStorageService != null)
                {
                    var localLanguage = await _localStorageService.GetItemAsync<string>("lang");
                    if (!string.IsNullOrEmpty(localLanguage))
                    {
                        _appStateManager.InvokeLanguageChanged(localLanguage);
                        lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
                    }
                }

                await RequestRender();
            }
            catch (JSDisconnectedException)
            {
                Console.WriteLine("⚠️ Circuit disconnected before JS call could complete.");
            }
        }
    }

    private async Task EditPageModalOpen(SellerProductViewModel selectedProduct)
    {
        try
        {
            if (selectedProduct == null)
                return;

            var parameters = new ModalParameters();
            parameters.Add(nameof(ProductDetailModal.EditableProduct), selectedProduct);
            parameters.Add(nameof(ProductDetailModal.ModalCloseCallBack),
                EventCallback.Factory.Create<bool>(this, CallbackModal));

            var options = new ModalOptions()
            {
                DisableBackgroundCancel = false,
                HideHeader = true,
                Size = ModalSize.Large,
                HideCloseButton = true,
                AnimationType = ModalAnimationType.FadeInOut
                
            };
            _openModal.Show<ProductDetailModal>(@lang["EditProductDetail"], parameters, options);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task CallbackModal(bool value)
    {
        if (value)
        {
            await GetProductList();
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
                Console.WriteLine($"⚠️ Home.AppState_StateChanged error: {ex.Message}");
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

    public void Dispose()
    {
        _isDisposed = true;
        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _appStateManager.StateChanged -= AppState_StateChanged;
    }

    private async Task ToggleFavorite(SellerProductViewModel product)
    {
        // Favorites bilgisi SellerProductViewModel'de yok, direkt service'e sor
        try
        {
            var favoritesResult = await FavoriteService.GetAllFavoritesAsync(1, 1000);
            bool isFavorite = favoritesResult.Ok == true && favoritesResult.Result?.Data?.Any(f => f.Id == product.ProductId) == true;
            
            if (isFavorite)
            {
                await FavoriteService.DeleteFavoriteForCurrentUserAsync(product.ProductId);
                _notificationService.Notify(NotificationSeverity.Info, lang["FavoriteRemoved"] ?? "Favoriden çıkarıldı");
            }
            else
            {
                await FavoriteService.UpsertFavoriteForCurrentUserAsync(product.ProductId);
                _notificationService.Notify(NotificationSeverity.Success, lang["FavoriteAdded"] ?? "Favorilere eklendi");
            }
            await RequestRender();
        }
        catch
        {
            _notificationService.Notify(NotificationSeverity.Error, lang["FavoriteError"] ?? "Favori işlemi başarısız oldu");
        }
    }    
   
    
}