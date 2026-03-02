using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Modal.Services;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Web.Domain.Services.Abstract;

namespace ecommerce.Web.Components.Modals;

public partial class ProductDetailModal
{
     [Parameter] public SellerProductViewModel EditableProduct { get; set; }
     [CascadingParameter] BlazoredModalInstance ModalInstance { get; set; }
     [CascadingParameter] public IModalService _openModal { get; set; }
     [Inject] private II18N lang { get; set; }
     [Inject] private ILocalStorageService _localStorage { get; set; }
     [Inject] private IJSRuntime _jsRuntime { get; set; }
     [Inject] private NotificationService _notificationService { get; set; }
     [Inject] private AppStateManager _appStateManager { get; set; }
     [Inject] protected ICartService _cartService { get; set; }
     [Parameter] public string ShowStyle { get; set; }
     [Parameter] public EventCallback<bool> ModalCloseCallBack { get; set; }
     [Inject] private CdnOptions CdnConfig{get;set;}

     protected override async Task OnAfterRenderAsync(bool firstRender)
     {
          if (firstRender)
          {
               var localLanguage = await _localStorage.GetItemAsync<string>("lang");
               if (localLanguage != null)
               {
                    _appStateManager.InvokeLanguageChanged(localLanguage);
                    lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
               }

               await _jsRuntime.InvokeVoidAsync("eval", "document.body.style.overflow = 'hidden'; window.scrollTo({ top: window.scrollY, behavior: 'instant' });");

               StateHasChanged();
          }
     }
     private async Task CloseModal()
     {
          if (ModalInstance != null)
          {
               await ModalInstance.CloseAsync();
               await ModalCloseCallBack.InvokeAsync(true);
          }
     }

     private async Task HandleAddToCart()
     {
          if (EditableProduct?.SellerItemId <= 0) return;

          await _appStateManager.ExecuteWithLoading(async () =>
          {
               var result = await _cartService.CreateCartItem(new ecommerce.Web.Domain.Dtos.Cart.CartItemUpsertDto
               {
                    Quantity = 1,
                    ProductSellerItemId = EditableProduct.SellerItemId,
                    SourceId = EditableProduct.SourceId
               });

               if (result.Ok)
               {
                    _notificationService.Notify(NotificationSeverity.Success, "Sepet", result.Metadata?.Message ?? "Sepet güncellendi");
                    await _appStateManager.UpdatedCart(null, result.Result);
                    await CloseModal();
               }
               else
               {
                    _notificationService.Notify(NotificationSeverity.Error, "Sepet Hatası", result.Metadata?.Message ?? lang["CartError"]);
               }
          }, "Sepet güncelleniyor");
     }
}