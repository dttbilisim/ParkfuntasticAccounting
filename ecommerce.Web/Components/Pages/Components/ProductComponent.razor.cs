using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Modal.Services;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Favorite;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Components.Modals;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Events;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
namespace ecommerce.Web.Components.Pages.Components;
public partial class ProductComponent : IDisposable{
    [CascadingParameter] public IModalService _openModal{get;set;}
    [Parameter] public List<ProductFavoriteDto> ? FavoriteProducts{get;set;} = new();
    [Parameter] public List<SellerProductViewModel> ? Product{get;set;} = new();
    [Parameter] public CartDto? CartResult { get; set; }
    [Parameter] public Func<CartItemUpsertDto, bool, Task>? OnCartUpdate { get; set; }
    [Parameter] public Func<Task>? OnFavoriteChanged { get; set; }
    [Inject] private CdnOptions CdnConfig{get;set;}
    [Inject] private IFavoriteService FavoriteService{get;set;}
    [Inject] private NotificationService _notificationService{get;set;}
    [Inject] private AppStateManager _appStateManager{get;set;}
    [Inject] private II18N lang{get;set;}
    [Inject] private IJSRuntime _jsruntime{get;set;}
    [Inject] private ILocalStorageService _localStorageService{get;set;}
    [Inject] protected ICartService _cartService{get;set;}
    private int currentUserId = 0;
    private int page = 1;
    private int pageSize = 20;
    private int totalCount = 0;
    
    private HashSet<int> BusyProducts{get;} = new();
    private int _lastDiscountCount = 0;
    private bool _shouldRender = true;
    
    protected override bool ShouldRender() => _shouldRender;
    
    private async Task BlastOnAppliedTransition(){
        var currentCount = CartResult?.AppliedDiscounts?.Count ?? 0;
        if(_lastDiscountCount == 0 && currentCount > 0){
            try{ await _jsruntime.InvokeVoidAsync("confettiEnsureBlast"); } catch { }
        }
        _lastDiscountCount = currentCount;
    }
    // Event handling moved to parent containers to prevent render storm
    
    protected override async Task OnInitializedAsync(){
        // CartResult value is now provided by parents
        if (CartResult == null)
        {
            CartResult = await _appStateManager.GetCart();
        }
    }
    private async Task ToggleFavorite(SellerProductViewModel product){
        // Favorites bilgisi SellerProductViewModel'de yok, direkt service'e sor
        await _appStateManager.ExecuteWithLoading(async () => {
            try{
                var result = await FavoriteService.GetAllFavoritesAsync(1, 1000);
                bool isFavorite = result.Ok == true && result.Result?.Data?.Any(f => f.Id == product.ProductId) == true;
                
                if(isFavorite){
                    await FavoriteService.DeleteFavoriteForCurrentUserAsync(product.ProductId);
                    _notificationService.Notify(NotificationSeverity.Info, "Favori", lang["FavoriteRemoved"] ?? "Favoriden çıkarıldı");
                } else{
                    await FavoriteService.UpsertFavoriteForCurrentUserAsync(product.ProductId);
                    _notificationService.Notify(NotificationSeverity.Success, "Favori", lang["FavoriteAdded"] ?? "Favorilere eklendi");
                }
                // StateHasChanged not needed - ExecuteWithLoading handles UI update
            } catch{
                _notificationService.Notify(NotificationSeverity.Error, "Favori Hatası", lang["FavoriteError"] ?? "Favori işlemi başarısız oldu");
            }
        }, "Favori işleniyor");
    }
    private async Task RemoveFromFavorite(int productId){
        await _appStateManager.ExecuteWithLoading(async () => {
            var result = await FavoriteService.DeleteFavoriteForCurrentUserAsync(productId);
            if(!result.Ok){
                _notificationService.Notify(NotificationSeverity.Error, "Favori Hatası", lang["FavoriteRemoveFailed"]);
            } else{
                _notificationService.Notify(NotificationSeverity.Success, "Favori", lang["FavoriteRemoved"]);
                

                if(OnFavoriteChanged != null){
                    await OnFavoriteChanged.Invoke();
                } else {
                    await LoadFavorites();
                }
            }
        }, "Favori kaldırılıyor");
    }
    private async Task LoadFavorites(){
        try{
            var result = await FavoriteService.GetAllFavoritesAsync(page, pageSize);
            if(result.Ok == true){
                var dataChanged = FavoriteProducts != result.Result.Data;
                FavoriteProducts = result.Result.Data;
                totalCount = result.Result.DataCount;
                if(dataChanged){
                    await InvokeAsync(StateHasChanged);
                }
            }
        } catch(Exception ex){
            Console.WriteLine($"HATA: Favori ürünler yüklenirken beklenmeyen bir hata oluştu. Detay: {ex.Message}");
        }
    }
    private Task EditPageModalOpen(SellerProductViewModel selectedProduct){
        try{
            if(selectedProduct == null){
                return Task.CompletedTask;
            }
            var parameters = new ModalParameters();
            parameters.Add(nameof(ProductDetailModal.EditableProduct), selectedProduct);
            var options = new ModalOptions(){
                DisableBackgroundCancel = false,
                HideHeader = true,
                Size = ModalSize.Large,
                HideCloseButton = true,
                AnimationType = ModalAnimationType.FadeInOut
            };
            _openModal.Show<ProductDetailModal>(@lang["EditProductDetail"], parameters, options);
        } catch(Exception e){
            Console.WriteLine($"HATA: Ürün detay modali açılırken sorun oluştu. Detay: {e.Message}");
            throw;
        }
        return Task.CompletedTask;
    }
    private async Task CartInsert(CartItemUpsertDto cartItem, bool isIncrease, int step = 1){
        try{
            var key = cartItem.ProductSellerItemId;
            if(key <= 0) return;
            if(BusyProducts.Contains(key)) return;
            BusyProducts.Add(key);
            cartItem.Quantity = (isIncrease ? 1 : -1) * step;

            await _appStateManager.ExecuteWithLoading(async () => {
                var result = await _cartService.CreateCartItem(cartItem);
                if(result.Ok){
                    _notificationService.Notify(NotificationSeverity.Success, "Sepet", result.Metadata?.Message ?? "Sepet güncellendi");
                    
                    // Centralized update: Parent will receive this and re-render this child with new CartResult parameter
                    await _appStateManager.UpdatedCart(this, result.Result); 

                    // Check for discount on the inserted item and trigger confetti
                    var currentCart = result.Result;
                    if (isIncrease && currentCart?.Sellers != null)
                    {
                        var insertedItem = currentCart.Sellers
                            .SelectMany(s => s.Items ?? Enumerable.Empty<CartItemDto>())
                            .FirstOrDefault(i => i.ProductSellerItemId == cartItem.ProductSellerItemId);

                        if (insertedItem != null && (insertedItem.DiscountAmount > 0 || insertedItem.AppliedDiscounts?.Any() == true))
                        {
                             try{ await _jsruntime.InvokeVoidAsync("confettiEnsureBlast"); } catch { }
                        }
                    }
                } else{
                    _notificationService.Notify(NotificationSeverity.Error, "Sepet Hatası", result.Metadata?.Message ?? lang["CartError"]);
                    // On error, still trigger update to ensure sync
                    await _appStateManager.UpdatedCart(this);
                    return;
                }
            }, "Sepet güncelleniyor");
        } catch(Exception ex){
            try{
                Console.WriteLine($"HATA: Sepete ürün eklenirken beklenmeyen bir hata oluştu. Detay: {ex.Message}");
            } catch{}
            _notificationService.Notify(NotificationSeverity.Error, "Sepet Hatası", lang["CartError"] ?? "İşlem sırasında bir hata oluştu.");
        } finally{
            BusyProducts.Remove(cartItem.ProductSellerItemId);
        }
    }

    private async Task AddToCart(ProductFavoriteDto product){
        if(product?.SellerItemId == null || product.SellerItemId <= 0){
            _notificationService.Notify(NotificationSeverity.Warning, "Stok Yok", "Bu ürün için stokta satıcı bulunamadı.");
            return;
        }

        await CartInsert(new CartItemUpsertDto{
            Quantity = 1,
            ProductSellerItemId = product.SellerItemId.Value,
            SourceId = product.SourceId
        }, true);
    }

    private async Task HandleProductAddToCart(SellerProductViewModel product){
        if(product?.SellerItemId <= 0) return;
        
        var qty = Math.Max(1, (int)product.MinSaleAmount);

        await CartInsert(new CartItemUpsertDto{
            Quantity = qty,
            ProductSellerItemId = product.SellerItemId,
            SourceId = product.SourceId
        }, true, qty); // Step is effectively the initial quantity here for handling "increase" logic internally if needed, but CartInsert overrides usage of step when we pass full object? No, logic is (isInc ? 1 : -1) * step. 
        // Wait, if I pass 'true' and 'qty', CartInsert will do 1 * qty = qty. This is correct for initial add.
    }
    public void Dispose(){
        // No longer subscribing to global events here
    }
    
    private static bool IsPerfectCompatibility(SellerProductViewModel? product){
        return product?.PerfectCompatibilityCars?.Any() == true;
    }

    private IReadOnlyList<string> GetMatchingCarSummaries(SellerProductViewModel? product){
        if(product?.PerfectCompatibilityCars == null || product.PerfectCompatibilityCars.Count == 0){
            return Array.Empty<string>();
        }

        return product.PerfectCompatibilityCars
            .Select(FormatCarSummary)
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string FormatCarSummary(SellerProductCompatibilityDto car){
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
            var fallbackSegments = new List<string>();
            if(!string.IsNullOrWhiteSpace(car.ManufacturerKey)){
                fallbackSegments.Add(car.ManufacturerKey.Trim().ToUpperInvariant());
            }
            if(!string.IsNullOrWhiteSpace(car.BaseModelKey)){
                fallbackSegments.Add(car.BaseModelKey.Trim().ToUpperInvariant());
            }
            if(!string.IsNullOrWhiteSpace(car.SubModelKey)){
                fallbackSegments.Add(car.SubModelKey.Trim().ToUpperInvariant());
            }

            if(fallbackSegments.Count > 0){
                modelSegments.Add(string.Join(" ", fallbackSegments));
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

    private string GetCompatibilityTitle(IReadOnlyList<string> matchingCars){
        if(matchingCars == null || matchingCars.Count == 0){
            return string.Empty;
        }
        return string.Join(" | ", matchingCars);
    }
}
