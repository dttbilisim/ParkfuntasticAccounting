using Blazored.LocalStorage;
using ecommerce.Web.Utility;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Events;
using Microsoft.AspNetCore.Components;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Core.Entities.Authentication;
using Blazored.Modal.Services;
using Blazored.Modal;
using ecommerce.Web.Components.Modals;
using ecommerce.Web.Domain.Dtos.Bank;
using ecommerce.Web.Domain.Dtos.Order;
using I18NPortable;
using Radzen;
using Microsoft.JSInterop;

namespace ecommerce.Web.Components.Pages;

public partial class CheckoutPage : IDisposable
{
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private NavigationManager _navManager { get; set; }
    [Inject] private ICartService _cartService { get; set; }
    [Inject] private IUserManager _userManager { get; set; }
    [Inject] private ICheckoutService _checkoutService { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private ILocalStorageService _localStorageService{get;set;}
    [Inject] private IBankService _bankService { get; set; }
    [Inject] private NotificationService _notificationService { get; set; }
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    [Inject] private IConfiguration _configuration { get; set; }
    [CascadingParameter] public IModalService _openModal { get; set; }

    private CartDto CartResult;
    private List<UserAddress> UserAddresses = new();
    private int? SelectedAddressId;
    private string couponCode;
    private bool isUpdatingCart;
    private bool _shouldRender = true;

    protected override bool ShouldRender() => _shouldRender;

    private List<BankListDto> Banks = new();
    private List<BankCardListDto> BankCards = new();
    private List<BankInstallmentListDto> Installments = new();
    
    private CardPaymentRequest PaymentModel = new CardPaymentRequest{CardNumber = "4912055023019402",ExpMonth = "09",ExpYear = "2026",Cvv = "642",CardHolderName = "Sezgin OZTEMIR"};
    private decimal CalculatedGrandTotal;
    private bool IsThreeDModalOpen;
    private DotNetObjectReference<CheckoutPage>? _objRef;
    public string? PaymentHtmlContent { get; set; }

    protected override async Task OnInitializedAsync()
    {
        CartResult = await _appStateManager.GetCart();
        if (CartResult == null || CartResult.TotalItems == 0)
        {
            _navManager.NavigateTo("/");
            return;
        }
        CalculatedGrandTotal = CartResult?.OrderTotal ?? 0;
        try
        {
            var rs = await _userManager.GetAllUserAddressesAsync();
            if (rs.Ok && rs.Result != null)
            {
                UserAddresses = rs.Result;
                SelectedAddressId = UserAddresses.FirstOrDefault()?.Id;
            }
            var uri = _navManager.ToAbsoluteUri(_navManager.Uri);
            if (Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("error", out var error))
            {
                 _notificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Ödeme İşlemi Başarısız",
                    Detail = error,
                    Duration = 5000
                });
            }

            // Load Banks
            await LoadBanks();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        _appStateManager.StateChanged += async (c, ev, updatedCart) =>
        {
            if (ev == AppStateEvents.updateCart)
            {
                if (CartResult != updatedCart)
                {
                    CartResult = updatedCart ?? CartResult ?? await _appStateManager.GetCart();
                    CalculatedGrandTotal = CartResult.OrderTotal;
                    if(PaymentModel.InstallmentId.HasValue) {
                         var inst = Installments.FirstOrDefault(x => x.Id == PaymentModel.InstallmentId.Value);
                         if(inst != null) {
                             CalculatedGrandTotal = CartResult.OrderTotal * (1 + (inst.InstallmentRate / 100));
                         }
                    }
                    _shouldRender = true;
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    _shouldRender = false;
                }
            }
        };
    }

    private async Task LoadBanks()
    {
        var result = await _bankService.GetActiveBanksAsync();
        if (result.Ok && result.Result != null)
        {
            Banks = result.Result;
        }
    }

    private async Task OnBankChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int bankId))
        {
            PaymentModel.BankId = bankId;
            PaymentModel.BankCardId = null;
            PaymentModel.InstallmentId = null;
            CalculatedGrandTotal = CartResult.OrderTotal;
            BankCards.Clear();
            Installments.Clear();

            var result = await _bankService.GetBankCardsAsync(bankId);
            if (result.Ok && result.Result != null && result.Result.Any())
            {
                BankCards = result.Result;
            }
        }
        else
        {
            PaymentModel.BankId = null;
            PaymentModel.BankCardId = null;
            PaymentModel.InstallmentId = null;
            BankCards.Clear();
            Installments.Clear();
        }
        _shouldRender = true;
        StateHasChanged();
    }

    private async Task OnBankCardChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int cardId))
        {
            await SelectBankCard(cardId);
        }
        else
        {
            PaymentModel.BankCardId = null;
            Installments.Clear();
        }
        _shouldRender = true;
        StateHasChanged();
    }

    public async Task SelectBankCard(int cardId)
    {
        PaymentModel.BankCardId = cardId;
        PaymentModel.InstallmentId = null;
        CalculatedGrandTotal = CartResult.OrderTotal;
        Installments.Clear();

        var result = await _bankService.GetBankInstallmentsAsync(cardId);
        if (result.Ok && result.Result != null && result.Result.Any())
        {
            Installments = result.Result;
        }
        _shouldRender = true;
    }

    private void OnInstallmentChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int instId))
        {
            SelectInstallment(instId);
        }
        else
        {
            PaymentModel.InstallmentId = null;
            CalculatedGrandTotal = CartResult.OrderTotal;
        }
        _shouldRender = true;
        StateHasChanged();
    }

    public void SelectInstallment(int instId)
    {
        if (instId == 0)
        {
            PaymentModel.InstallmentId = null;
            CalculatedGrandTotal = CartResult.OrderTotal;
        }
        else
        {
            PaymentModel.InstallmentId = instId;
            var inst = Installments.FirstOrDefault(x => x.Id == instId);
            if (inst != null)
            {
                CalculatedGrandTotal = CartResult.OrderTotal * (1 + (inst.InstallmentRate / 100));
            }
        }
        _shouldRender = true;
    }

    private async Task Checkout()
    {
        if (!SelectedAddressId.HasValue)
        {            
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = lang["Checkout.AddressRequired"],
                Detail = lang["Checkout.PleaseSelectAddress"],
                Duration = 4000
            });
            return;
        }
        
        var requirePaymentPage = _configuration.GetValue<bool>("Payment:RequirePaymentPage", true);
        
        if (!requirePaymentPage)
        {
            await _appStateManager.ExecuteWithLoading(async () =>
            {
                var request = new CheckoutRequestDto
                {
                    UserAddressId = SelectedAddressId,
                    CardPayment = null
                };
                
                var result = await _checkoutService.Checkout(request);
                if (result.Ok && result.Result != null)
                {
                    var orderNumber = result.Result.OrderNumbers?.FirstOrDefault() ?? "N/A";
                    _notificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Sipariş Oluşturuldu",
                        Detail = $"Siparişiniz başarıyla oluşturuldu. Sipariş No: {orderNumber}",
                        Duration = 5000
                    });
                    _navManager.NavigateTo($"/user-dashboard?success=true&orderNumber={orderNumber}#pills-order", true);
                }
                else
                {
                    var errorMsg = result.Metadata?.Message ?? "Sipariş oluşturulurken beklenmedik bir hata oluştu.";
                    _notificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Sipariş Başarısız",
                        Detail = errorMsg,
                        Duration = 5000
                    });
                }
            }, "Sipariş oluşturuluyor...");
            return;
        }
        
        if (!PaymentModel.BankId.HasValue)
        {
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Banka Seçimi",
                Detail = "Lütfen bir banka seçiniz.",
                Duration = 4000
            });
            return;
        }

        if(string.IsNullOrWhiteSpace(PaymentModel.CardHolderName) ||
           string.IsNullOrWhiteSpace(PaymentModel.CardNumber) ||
           string.IsNullOrWhiteSpace(PaymentModel.ExpMonth) ||
           string.IsNullOrWhiteSpace(PaymentModel.ExpYear) || 
           string.IsNullOrWhiteSpace(PaymentModel.Cvv))
        {
             _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Kart Bilgileri",
                Detail = "Lütfen tüm kart bilgilerini eksiksiz giriniz.",
                Duration = 4000
            });
            return;
        }

        await _appStateManager.ExecuteWithLoading(async () =>
        {
            var request = new CheckoutRequestDto
            {
                UserAddressId = SelectedAddressId,
                CardPayment = PaymentModel
            };
            
            var result = await _checkoutService.Checkout(request);
            if (result.Ok)
            {
                if(!string.IsNullOrEmpty(result.Result.CheckoutFormContent))
               {
                   IsThreeDModalOpen = true;
                   PaymentHtmlContent = result.Result.CheckoutFormContent;
                   _shouldRender = true;
                   await InvokeAsync(StateHasChanged);
                   await Task.Delay(100); 
                   
                   try {
                       await _jsRuntime.InvokeVoidAsync("window.checkoutHelpers.submitForm");
                   } catch(Exception ex) {
                       Console.WriteLine("JS Submit Error: " + ex.Message);
                   }
               }
            }
            else
            {
                var errorMsg = result.Metadata?.Message ?? "Ödeme işlemi sırasında beklenmedik bir hata oluştu.";
                _notificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Ödeme Başarısız",
                    Detail = errorMsg,
                    Duration = 5000
                });
            }
        }, "İşleminizi Yapıyorum Bekleyiniz.");
    }

    private async Task Close3DModal()
    {
        IsThreeDModalOpen = false;
        await _checkoutService.OrderDelete();
        _shouldRender = true;
    }

    [JSInvokable]
    public async Task ProcessPaymentResult(string status, string messageOrOrderNumber)
    {
        IsThreeDModalOpen = false;
        _shouldRender = true;
        await InvokeAsync(StateHasChanged);

        if (status == "success")
        {
             _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Ödeme Başarılı",
                Detail = $"Siparişiniz başarıyla alındı. Sipariş No: {messageOrOrderNumber}",
                Duration = 5000
            });
            _navManager.NavigateTo($"/user-dashboard?success=true&orderNumber={messageOrOrderNumber}#pills-order", true);
        }
        else
        {
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Ödeme Başarısız",
                Detail = messageOrOrderNumber,
                Duration = 5000
            });
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if(firstRender){
                _objRef = DotNetObjectReference.Create(this);
                await _jsRuntime.InvokeVoidAsync("eval", @"
                    window.checkoutHelpers = {
                        registerListener: function(dotNetHelper) {
                            window.checkoutPageRef = dotNetHelper;
                            window.addEventListener('message', function(event) {
                                if (event.data && event.data.type === 'paymentResult') {
                                    if(window.checkoutPageRef) {
                                        window.checkoutPageRef.invokeMethodAsync('ProcessPaymentResult', event.data.status, event.data.message || event.data.orderNumber);
                                    }
                                }
                            });
                        },
                        submitForm: function() {
                            var container = document.getElementById('payment-form-container');
                            var form = null;
                            if (container) { form = container.querySelector('form'); }
                            if (!form) { form = document.getElementById('PaymentForm'); }
                            if(form) { form.submit(); return true; } 
                            return false;
                        }
                    };
                ");
                await _jsRuntime.InvokeVoidAsync("window.checkoutHelpers.registerListener", _objRef);

                var localLanguage = await _localStorageService.GetItemAsync<string>("lang");
                if(localLanguage != null){
                    _appStateManager.InvokeLanguageChanged(localLanguage);
                    lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
                }
                CartResult = await _appStateManager.GetCart();
                _shouldRender = true;
                StateHasChanged();
            }
        } catch(Exception e){
            Console.WriteLine(e);
        }
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }

    private async Task OpenNewAddressModal()
    {
        var parameters = new ModalParameters();
        UserAddress draft = new();
        try
        {
            var userRs = await _userManager.GetCurrentUserAsync();
            if (userRs.Ok && userRs.Result != null)
            {
                draft.FullName = userRs.Result.FullName ?? string.Empty;
                draft.Email = userRs.Result.Email ?? string.Empty;
                draft.PhoneNumber = userRs.Result.PhoneNumber ?? string.Empty;
                draft.AddressName = "Yeni Adres";
            }
        }
        catch { }
        parameters.Add(nameof(AddOrUpdateAdressModal.EditableAddress), draft);
        var options = new ModalOptions
        {
            DisableBackgroundCancel = false,
            HideHeader = true,
            Size = ModalSize.Large,
            HideCloseButton = true,
            AnimationType = ModalAnimationType.FadeInOut
        };
        var modalRef = _openModal.Show<AddOrUpdateAdressModal>("Yeni Adres", parameters, options);
        var result = await modalRef.Result;
        if (!result.Cancelled)
        {
            var rs = await _userManager.GetAllUserAddressesAsync();
            if (rs.Ok && rs.Result != null)
            {
                UserAddresses = rs.Result;
                SelectedAddressId = UserAddresses.OrderByDescending(a => a.Id).FirstOrDefault()?.Id;
                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task RemoveItem(int cartItemId)
    {
        if (isUpdatingCart) return;
        isUpdatingCart = true;
        try
        {
            await _appStateManager.ExecuteWithLoading(async () => {
                var rs = await _cartService.CartItemRemove(cartItemId);
                await _appStateManager.UpdatedCart(this, null);
                CartResult = await _appStateManager.GetCart();
                if (CartResult == null || CartResult.TotalItems == 0)
                {
                    _navManager.NavigateTo("/");
                    return;
                }
                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }, lang["Checkout.RemovingProduct"] ?? "Ürün kaldırılıyor");
        }
        finally
        {
            isUpdatingCart = false;
        }
    }

    private async Task ClearCart()
    {
        bool confirmed = await _jsRuntime.InvokeAsync<bool>("swalConfirm", 
            lang["Checkout.ClearCartConfirmTitle"] ?? "Sepeti Temizle", 
            lang["Checkout.ClearCartConfirmText"] ?? "Sepetinizdeki tüm ürünler silinecektir. Emin misiniz?",
            "warning");

        if (!confirmed) return;

        await _appStateManager.ExecuteWithLoading(async () => {
            var rs = await _cartService.ClearCart();
            if (rs.Ok)
            {
                await _appStateManager.UpdatedCart(this, null);
                _navManager.NavigateTo("/");
            } else {
                CartResult = await _appStateManager.GetCart();
                _shouldRender = true;
                await InvokeAsync(StateHasChanged);
            }
        }, lang["Checkout.ClearingCart"] ?? "Sepet temizleniyor");
    }

    private async Task ApplyCoupon()
    {
        var raw = (couponCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;
        
        await _appStateManager.ExecuteWithLoading(async () => {
            var prefs = await _appStateManager.GetCartPreferences();
            prefs.UsedCouponCode = raw;
            await _appStateManager.SetCartPreferences(prefs);
            await _appStateManager.UpdatedCart(this, null);
            CartResult = await _appStateManager.GetCart();
            if (CartResult.IsCouponCodeApplied)
            {
                couponCode = string.Empty;
            }
            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }, "Kupon uygulanıyor");
    }

    private async Task RemoveCoupon()
    {
        await _appStateManager.ExecuteWithLoading(async () => {
            var prefs = await _appStateManager.GetCartPreferences();
            prefs.UsedCouponCode = null;
            await _appStateManager.SetCartPreferences(prefs);
            await _appStateManager.UpdatedCart(this, null);
            CartResult = await _appStateManager.GetCart();
            _shouldRender = true;
            await InvokeAsync(StateHasChanged);
        }, "Kupon kaldırılıyor");
    }
}
