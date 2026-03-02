using Blazored.LocalStorage;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Events;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Radzen;
using Microsoft.JSInterop;
namespace ecommerce.Web.Components.Pages;
public partial class CartPage{
    [Inject] private AppStateManager _appStateManager{get;set;}
    [Inject] private ICartService _cartService{get;set;}
    [Inject] private II18N lang{get;set;}
    [Inject] private NavigationManager _navigationManager{get;set;}
    [Inject] private ILocalStorageService _localStorageService{get;set;}
    [Inject] private NotificationService _notificationService{get;set;}
    [Inject] private ICookieManager _cookieManager{get;set;}
    [Inject] private IJSRuntime _jsruntime{get;set;}
    [Inject] private DialogService _dialogService{get;set;}
    private CartDto CartResult = new();
    private string couponCode = string.Empty;
    private HashSet<int> BusyProducts = new();
    private bool _lastCouponApplied = false;
    private bool _shouldRender = true;
    
    protected override bool ShouldRender() => _shouldRender;
    protected override async Task OnInitializedAsync(){
        CartResult = await _appStateManager.GetCart();
        _appStateManager.StateChanged += async (c, ev, updatedCart) => {
            if(ev == AppStateEvents.updateCart){
                if(ReferenceEquals(c, this)){
                    return;
                }
                var oldCart = CartResult;
                _shouldRender = false;
                CartResult = updatedCart ?? CartResult ?? await _appStateManager.GetCart();
                _shouldRender = !ReferenceEquals(oldCart, CartResult);
                if(_shouldRender){
                    await InvokeAsync(StateHasChanged);
                }
            }
        };
    }
    private async Task UpdateQty(int productSellerItemId, bool isIncrease){
        if(productSellerItemId <= 0) return;
        if(BusyProducts.Contains(productSellerItemId)) return;
        
        var item = CartResult.Sellers?.SelectMany(s => s.Items).FirstOrDefault(i => i.ProductSellerItemId == productSellerItemId);
        if(item == null) return;
        
        var step = (int)Math.Max(1, item.Step);
        var minQty = (int)Math.Max(1, item.MinSellCount);
        var currentQty = item.Quantity;
        
        // If at minimum and decreasing, remove all items
        var qtyToChange = isIncrease ? step : (currentQty <= minQty ? currentQty : step);
        var delta = (isIncrease ? 1 : -1) * qtyToChange;
        
        BusyProducts.Add(productSellerItemId);
        try{
            await _appStateManager.ExecuteWithLoading(async () => {
                    var req = new CartItemUpsertDto{ProductSellerItemId = productSellerItemId, Quantity = delta};
                    var rs = await _cartService.CreateCartItem(req);
                    if(rs.Ok){
                        await _appStateManager.UpdatedCart(this, rs.Result);
                        CartResult = rs.Result ?? await _appStateManager.GetCart();
                        _notificationService.Notify(NotificationSeverity.Success, "Sepet", rs.Metadata?.Message ?? rs.GetMetadataMessages() ?? "Sepet başarıyla güncellendi");
                        // StateHasChanged handled by UpdatedCart event
                        return;
                    } else{
                        _notificationService.Notify(NotificationSeverity.Error, "Sepet Hatası", rs.Metadata?.Message ?? rs.GetMetadataMessages() ?? "Sepet güncellenirken hata oluştu");
                        return;
                    }
                }, "Sepet güncelleniyor"
            );
        } finally{
            BusyProducts.Remove(productSellerItemId);
        }
    }
    private async Task RemoveItem(int cartItemId){
        await _appStateManager.ExecuteWithLoading(async () => {
                var rs = await _cartService.CartItemRemove(cartItemId);
                if(rs.Ok){
                    await _appStateManager.UpdatedCart(this, rs.Result);
                    CartResult = rs.Result ?? await _appStateManager.GetCart();
                    _notificationService.Notify(NotificationSeverity.Success, "Sepet", rs.Metadata?.Message ?? "Ürün sepetten kaldırıldı");
                    // StateHasChanged handled by UpdatedCart event
                } else{
                    _notificationService.Notify(NotificationSeverity.Error, "Sepet Hatası", rs.Metadata?.Message ?? "Ürün kaldırılırken hata oluştu");
                }
            }, "Ürün kaldırılıyor"
        );
    }
    protected override async Task OnAfterRenderAsync(bool firstRender){
        try{
            if(firstRender){
                var localLanguage = await _localStorageService.GetItemAsync<string>("lang");
                if(localLanguage != null){
                    _appStateManager.InvokeLanguageChanged(localLanguage);
                    lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
                }
                CartResult = await _appStateManager.GetCart();
                // StateHasChanged triggered below after language change if needed
            }
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
    }
    private async Task OnQtyInput(int productSellerItemId, ChangeEventArgs e){
        if(int.TryParse(e.Value?.ToString(), out var newQty)){
            if(newQty < 0) newQty = 0;
            foreach(var seller in CartResult.Sellers){
                var it = seller.Items.FirstOrDefault(i => i.ProductSellerItemId == productSellerItemId);
                if(it != null){
                    it.Quantity = newQty;
                    if(newQty == 0) seller.Items.Remove(it);
                    break;
                }
            }
            CartResult.Sellers.RemoveAll(s => !s.Items.Any());
            // Update app state which will trigger render via event
            await _appStateManager.UpdatedCart(this, null);
            CartResult = await _appStateManager.GetCart();
        }
    }
    private async Task CouponApply(){
        var raw = (couponCode ?? string.Empty).Trim();
        if(string.IsNullOrWhiteSpace(raw)){
            _notificationService.Notify(NotificationSeverity.Warning, "Kupon", "Lütfen kupon kodu girin.");
            return;
        }
        var cartPreferences = await _appStateManager.GetCartPreferences();
        cartPreferences.UsedCouponCode = raw;
        await _appStateManager.SetCartPreferences(cartPreferences);
        await ReloadCart();
        if(CartResult.IsCouponCodeApplied){
            couponCode = "";
            _notificationService.Notify(NotificationSeverity.Success, "Kupon", "Kupon kodu başarıyla uygulandı.");
            try{
                await InvokeAsync(async () => await _jsruntime.InvokeVoidAsync("confettiEnsureBlast"));
            } catch{}
        } else{
            cartPreferences.UsedCouponCode = null;
            await _appStateManager.SetCartPreferences(cartPreferences);
            // await ReloadCart();
            _notificationService.Notify(NotificationSeverity.Error, "Kupon Hatası", "Kupon kodu geçerli değil.");
        }
    }
    private async Task ReloadCart(){
        var cartResult = await _appStateManager.GetCart();
        CartResult = cartResult;
        await _appStateManager.UpdatedCart(this, CartResult);
        // StateHasChanged handled by UpdatedCart event
    }
    private async Task CouponDelete(){
        var confirmed = await _dialogService.Confirm("Kupon kodunu silmek istiyor musunuz?", "Onay", new ConfirmOptions{OkButtonText = "Evet", CancelButtonText = "İptal"});
        if(confirmed == false){
            return;
        }
        var cartPreferences = await _appStateManager.GetCartPreferences();
        cartPreferences.UsedCouponCode = null;
        await _appStateManager.SetCartPreferences(cartPreferences);
        await _appStateManager.UpdatedCart(this, null);
        CartResult = await _appStateManager.GetCart();
        _notificationService.Notify(NotificationSeverity.Info, "Kupon", "Kupon kodu kaldırıldı.");
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnSellerCheckboxChanged(int sellerId, ChangeEventArgs e)
    {
        var isChecked = (bool)(e.Value ?? false);
        
        await _appStateManager.ExecuteWithLoading(async () => {
            var response = await _cartService.PassiveCartItemBySellerId(sellerId, isChecked);
            
            if(response.Ok){
                CartResult = response.Result ?? await _appStateManager.GetCart();
                await _appStateManager.UpdatedCart(this, CartResult);
                
                var message = isChecked 
                    ? "Satıcı ürünleri sepete eklendi" 
                    : "Satıcı ürünleri sepetten çıkarıldı";
                
                _notificationService.Notify(
                    NotificationSeverity.Success, 
                    "Sepet", 
                    response.Metadata?.Message ?? message
                );
                
                await InvokeAsync(StateHasChanged);
            } else {
                _notificationService.Notify(
                    NotificationSeverity.Error, 
                    "Sepet Hatası", 
                    response.Metadata?.Message ?? "Sepet güncellenirken hata oluştu"
                );
            }
        }, "Sepet güncelleniyor...");
    }

    private async Task OnProductCheckboxChanged(int productSellerItemId, ChangeEventArgs e)
    {
        var isChecked = (bool)(e.Value ?? false);
        
        await _appStateManager.ExecuteWithLoading(async () => {
            var response = await _cartService.PassiveCartItemByProductSellerItemId(productSellerItemId, isChecked);
            
            if(response.Ok){
                CartResult = response.Result ?? await _appStateManager.GetCart();
                await _appStateManager.UpdatedCart(this, CartResult);
                
                var message = isChecked 
                    ? "Ürün sepete eklendi" 
                    : "Ürün sepetten çıkarıldı";
                
                _notificationService.Notify(
                    NotificationSeverity.Success, 
                    "Sepet", 
                    response.Metadata?.Message ?? message
                );
                
                await InvokeAsync(StateHasChanged);
            } else {
                _notificationService.Notify(
                    NotificationSeverity.Error, 
                    "Sepet Hatası", 
                    response.Metadata?.Message ?? "Ürün güncellenirken hata oluştu"
                );
            }
        }, "Ürün güncelleniyor...");
    }

    private async Task OnSellerCargoChangedAsync(CartSellerDto seller, CartCargoDto cargo)
    {
        seller.SelectedCargo = cargo;
        
        var cartPreferences = await _appStateManager.GetCartPreferences();
        if (cartPreferences.SelectedCargoes.ContainsKey(seller.SellerId))
        {
            cartPreferences.SelectedCargoes[seller.SellerId] = cargo.CargoId;
        }
        else
        {
            cartPreferences.SelectedCargoes.Add(seller.SellerId, cargo.CargoId);
        }
        
        await _appStateManager.SetCartPreferences(cartPreferences);
        await _appStateManager.UpdatedCart(this, null);
    }
}
