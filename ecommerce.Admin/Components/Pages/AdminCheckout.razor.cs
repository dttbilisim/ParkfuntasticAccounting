using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Dtos.Order;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils;
using Radzen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ecommerce.Admin.Components.Pages
{
    public partial class AdminCheckout : IDisposable
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private ICartStateService CartStateService { get; set; } = null!;
        [Inject] private ICustomerService CustomerService { get; set; } = null!;
        [Inject] private ecommerce.Web.Domain.Services.Abstract.ICheckoutService CheckoutService { get; set; } = null!;
        [Inject] private ecommerce.Admin.Services.AuthenticationService Security { get; set; } = null!;
        [Inject] private NotificationService NotificationService { get; set; } = null!;
        [Inject] private DialogService DialogService { get; set; } = null!;
        [Inject] private TooltipService TooltipService { get; set; } = null!;
        [Inject] private Microsoft.Extensions.Configuration.IConfiguration Configuration { get; set; } = null!;
        [Inject] private ecommerce.Web.Domain.Services.Abstract.ICartService CartService { get; set; } = null!;
        [Inject] private Microsoft.AspNetCore.Http.IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
        [Inject] private ecommerce.Web.Domain.Services.Abstract.IBankService BankService { get; set; } = null!;
        [Inject] private Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] private IPaymentModalService PaymentModalService { get; set; } = null!;
        [Inject] private Microsoft.Extensions.Logging.ILogger<AdminCheckout> Logger { get; set; } = null!;
        [Inject] private IDashboardCacheService DashboardCache { get; set; } = null!;

        private CartDto? CurrentCart;
        private CustomerUpsertDto? CustomerInfo;
        private List<ecommerce.Admin.Domain.Dtos.UserAddressDto.UserAddressListDto> CustomerAddresses { get; set; } = new();
        private int? SelectedUserAddressId { get; set; }
        private string OrderNote = string.Empty;
        private bool isLoading = true;
        private bool isProcessing = false;
        private bool isUpdatingCart = false;
        private HashSet<int> updatingItems = new();
        private ecommerce.Core.Entities.CartCustomerSavedPreferences CartPreferences = new();
        
        // Payment Modal Control
        private bool ShouldShowPaymentModal = false; // CustomerWorkingType == Pesin (1) ise true
        
        // Payment Data
        private List<ecommerce.Web.Domain.Dtos.Bank.BankListDto> Banks = new();
        private List<ecommerce.Web.Domain.Dtos.Bank.BankCardListDto> BankCards = new();
        private List<ecommerce.Web.Domain.Dtos.Bank.BankInstallmentListDto> Installments = new();
        
        // Installment dropdown items for RadzenDropDown
        private List<InstallmentDropdownItem> InstallmentDropdownItems = new();
        
        private class InstallmentDropdownItem
        {
            public int? Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }
        private ecommerce.Web.Domain.Dtos.Order.CardPaymentRequest PaymentModel = new ecommerce.Web.Domain.Dtos.Order.CardPaymentRequest{CardNumber = "4912055023019402",ExpMonth = "09",ExpYear = "2026",Cvv = "642",CardHolderName = "Sezgin OZTEMIR"};
        private decimal CalculatedGrandTotal = 0;
        private decimal InterestDifference = 0;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Check if user is authenticated
                if (Security.User == null)
                {
                    NavigationManager.NavigateTo("/login");
                    return;
                }

                // Get current cart
                // Try to load initial preferences from cookies if HttpContext is available
                var httpContext = HttpContextAccessor.HttpContext;
                if (httpContext != null && httpContext.Request.Cookies.ContainsKey("Cart"))
                {
                    try
                    {
                        var cookieVal = httpContext.Request.Cookies["Cart"];
                        if (!string.IsNullOrEmpty(cookieVal))
                        {
                            CartPreferences = Newtonsoft.Json.JsonConvert.DeserializeObject<ecommerce.Core.Entities.CartCustomerSavedPreferences>(cookieVal) 
                                             ?? new ecommerce.Core.Entities.CartCustomerSavedPreferences();
                        }
                    }
                    catch { }
                }

                await CartStateService.RefreshCart(CartPreferences);
                CurrentCart = CartStateService.CurrentCart;
                
                // If cart is empty, redirect to product search
                if (CurrentCart == null || CurrentCart.TotalItems == 0)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Sepet Boş",
                        Detail = "Sipariş vermek için sepetinize ürün eklemeniz gerekmektedir.",
                        Duration = 4000
                    });
                    NavigationManager.NavigateTo("/product-search");
                    return;
                }

                // Load customer info (B2B uses Customer entity, not UserAddress)
                await LoadCustomerInfo();
                
                // Load customer addresses if available
                var hasActiveCustomer = Security.User?.CustomerId.HasValue == true || Security.SelectedCustomerId.HasValue;
                if (hasActiveCustomer)
                {
                    await LoadCustomerAddresses();
                }

                // Subscribe to cart state changes
                CartStateService.OnChange += OnCartStateChanged;
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Sayfa yüklenirken bir hata oluştu.",
                    Duration = 4000
                });
            }
            finally
            {
                isLoading = false;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    var objRef = Microsoft.JSInterop.DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync("eval", @"
                        if (!window.checkoutHelpers) {
                            window.checkoutHelpers = {
                                registerListener: function(dotNetHelper) {
                                    if (window.adminCheckoutListenerAdded) {
                                        console.log('3D Secure listener already registered, re-pointing dotNetHelper');
                                        window.adminCheckoutPageRef = dotNetHelper;
                                        return;
                                    }
                                    window.adminCheckoutPageRef = dotNetHelper;
                                    window.addEventListener('message', function(event) {
                                        if (event.data && event.data.type === 'paymentResult') {
                                            if(window.adminCheckoutPageRef) {
                                                console.log('Payment result received:', event.data);
                                                window.adminCheckoutPageRef.invokeMethodAsync('ProcessPaymentResult', event.data.status, event.data.message || event.data.orderNumber);
                                            }
                                        }
                                    });
                                    window.adminCheckoutListenerAdded = true;
                                    console.log('3D Secure listener registered');
                                },
                                submitForm: function() {
                                    console.log('submitForm: Looking for payment form...');
                                    var container = document.getElementById('payment-form-container');
                                    var form = null;
                                    if (container) { 
                                        form = container.querySelector('form'); 
                                        console.log('submitForm: Container found, form:', form != null);
                                    }
                                    if (!form) { 
                                        form = document.getElementById('PaymentForm'); 
                                        console.log('submitForm: Fallback check, form:', form != null);
                                    }
                                    if(form) { 
                                        // CRITICAL: Ensure target matches iframe name
                                        form.target = 'threeDIframe';
                                        console.log('submitForm: Submitting form to threeDIframe...');
                                        form.submit(); 
                                        return true; 
                                    } 
                                    console.error('submitForm: No form found!');
                                    return false;
                                }
                            };
                        }
                    ");
                    await JSRuntime.InvokeVoidAsync("window.checkoutHelpers.registerListener", objRef);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Listener Init Error: " + ex.Message);
                }
            }
        }

        private async Task LoadCustomerInfo()
        {
            try
            {
                // B2B users (ApplicationUser) have CustomerId linking to Customer entity
                // If acting as Plasiyer, use SelectedCustomerId
                var activeCustomerId = Security.SelectedCustomerId ?? Security.User?.CustomerId;
                
                if (activeCustomerId == null)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Müşteri Bilgisi Eksik",
                        Detail = "Hesabınıza müşteri bilgisi atanmamış. Lütfen yöneticinizle iletişime geçiniz.",
                        Duration = 5000
                    });
                    return;
                }

                // Load customer entity which contains address information
                var result = await CustomerService.GetCustomerById(activeCustomerId.Value);
                
                if (result.Ok && result.Result != null)
                {
                    CustomerInfo = result.Result;
                    
                    // Check CustomerWorkingType: If Pesin (1) or PesinAndVadeli (3), initialize payment model but don't load banks on page
                    ShouldShowPaymentModal = CustomerInfo.CustomerWorkingType == CustomerWorkingTypeEnum.Pesin 
                                           || CustomerInfo.CustomerWorkingType == CustomerWorkingTypeEnum.PesinAndVadeli;
                                           
                    if (ShouldShowPaymentModal)
                    {
                        CalculatedGrandTotal = CurrentCart?.OrderTotal ?? 0;
                        
                        PaymentModel = new ecommerce.Web.Domain.Dtos.Order.CardPaymentRequest
                        {
                            CardNumber = "4912055023019402",
                            ExpMonth = "09",
                            ExpYear = "2040",
                            Cvv = "642",
                            CardHolderName = "Sezgin OZTEMIR"
                        };
                    }
                    
                    Logger.LogInformation("Customer loaded: {CustomerId}, WorkingType: {WorkingType}, ShouldShowPaymentModal: {ShouldShowPaymentModal}", 
                        CustomerInfo.Id, CustomerInfo.CustomerWorkingType, ShouldShowPaymentModal);
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = "Müşteri bilgileri yüklenirken bir hata oluştu.",
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
                    Detail = "Müşteri bilgisi yüklenirken bir hata oluştu.",
                    Duration = 4000
                });
            }
        }

        private async Task LoadCustomerAddresses()
        {
            try
            {
                var activeCustomerId = Security.SelectedCustomerId ?? Security.User?.CustomerId;
                if (activeCustomerId.HasValue != true) return;
                
                var result = await CustomerService.GetCustomerAddresses(activeCustomerId.Value);
                if (result.Ok && result.Result != null)
                {
                    CustomerAddresses = result.Result;
                    
                    // Auto-select default address
                    var defaultAddress = CustomerAddresses.FirstOrDefault(a => a.IsDefault);
                    if (defaultAddress != null)
                    {
                        SelectedUserAddressId = defaultAddress.Id;
                    }
                    else if (CustomerAddresses.Any())
                    {
                        // If no default, select first one
                        SelectedUserAddressId = CustomerAddresses.First().Id;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail - addresses are optional
            }
        }

        private async Task CompleteOrder()
        {
            if (isProcessing)
            {
                return;
            }

            try
            {
                isProcessing = true;
                await InvokeAsync(StateHasChanged);

                // Validate customer info exists
                if (CustomerInfo == null)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Uyarı",
                        Detail = "Müşteri bilgileri bulunamadı.",
                        Duration = 3000
                    });
                    return;
                }

                if (CurrentCart == null || CurrentCart.TotalItems == 0)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Uyarı",
                        Detail = "Sepetinizde ürün bulunmamaktadır.",
                        Duration = 3000
                    });
                    return;
                }

                Logger.LogInformation("CompleteOrder triggered: WorkingType={WorkingType}", CustomerInfo.CustomerWorkingType);

                // Handle payment based on CustomerWorkingType
                if (CustomerInfo.CustomerWorkingType == CustomerWorkingTypeEnum.Pesin)
                {
                    Logger.LogInformation("Branched to Type 1 (Pesin) - Opening Modal");
                    
                    // Reset processing so user can interact with modal
                    isProcessing = false;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(150); // Ensure overlay is removed from DOM before showing modal
                    
                    // Type 1: Always Online Payment - Show Modal
                    var result = await OpenBankSelectionDialog();
                    if (result == true)
                    {
                        Logger.LogInformation("Modal returned success - Processing Payment");
                        isProcessing = true;
                        await InvokeAsync(StateHasChanged);
                        await ProcessPayment();
                    }
                    return;
                }
                else if (CustomerInfo.CustomerWorkingType == CustomerWorkingTypeEnum.PesinAndVadeli)
                {
                    // Reset processing for interaction
                    isProcessing = false;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(150); // Give time for overlay to disappear

                    // Type 3: Ask for preference
                    var paymentChoice = await DialogService.Confirm(
                        "Ödeme yöntemini seçiniz",
                        "Ödeme Seçimi",
                        new ConfirmOptions 
                        { 
                            OkButtonText = "Online Sanal Ödeme", 
                            CancelButtonText = "Cari Hesap" 
                        });
                    
                    if (paymentChoice == true)
                    {
                        // User chose Online Payment - Show Modal
                        var result = await OpenBankSelectionDialog();
                        if (result == true)
                        {
                            Logger.LogInformation("Modal returned success - Processing Payment");
                            isProcessing = true;
                            await InvokeAsync(StateHasChanged);
                            await ProcessPayment();
                        }
                        return;
                    }
                    else if (paymentChoice == false)
                    {
                        // User chose Cari Hesap
                        isProcessing = true;
                        await InvokeAsync(StateHasChanged);
                        await PerformCheckoutDirectly();
                        return;
                    }
                    else
                    {
                        // User cancelled choice - do nothing
                        return;
                    }
                }

                // Default logic (Vadeli or other types)
                await PerformCheckoutDirectly();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during CompleteOrder");
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Sipariş işlenirken bir hata oluştu: " + ex.Message,
                    Duration = 4000
                });
            }
            finally
            {
                isProcessing = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task<bool?> OpenBankSelectionDialog()
        {
            var result = await DialogService.OpenAsync<ecommerce.Admin.Components.Pages.Modals.BankSelectionDialog>("Banka ve Taksit Seçimi",
                new Dictionary<string, object>
                {
                    { "PaymentModel", PaymentModel },
                    { "OrderTotal", CurrentCart?.OrderTotal ?? 0 }
                },
                new DialogOptions { Width = "500px", Height = "auto", Resizable = false, Draggable = false });
            
            return result;
        }

        private async Task PerformCheckoutDirectly()
        {
            var activeCustomerId = Security.SelectedCustomerId ?? Security.User?.CustomerId;
            Logger.LogInformation("=== AdminCheckout.PerformCheckoutDirectly STARTED === CustomerId: {CustomerId}, CartItems: {CartItems}, SelectedAddressId: {AddressId}",
                activeCustomerId, CurrentCart?.TotalItems, SelectedUserAddressId);
            
            isProcessing = true;
            StateHasChanged();



            try 
            {
                // Create checkout request - use selected UserAddress if available
                var checkoutRequest = new CheckoutRequestDto
                {
                    UserAddressId = SelectedUserAddressId,
                    CardPayment = null,
                    PlatformType = OrderPlatformType.B2B,
                    OnBehalfOfCustomerId = activeCustomerId
                };

                var result = await CheckoutService.Checkout(checkoutRequest);
                
                if (result != null && result.Ok && result.Result != null)
                {
                    var orderNumbers = result.Result.OrderNumbers != null && result.Result.OrderNumbers.Any() 
                        ? string.Join(", ", result.Result.OrderNumbers) 
                        : "N/A";
                    
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Sipariş Oluşturuldu",
                        Detail = $"Siparişiniz başarıyla oluşturuldu. Sipariş No: {orderNumbers}",
                        Duration = 5000
                    });

                    // Invalidate dashboard cache for the plasiyer (total view) and the specific customer
                    DashboardCache.InvalidateCache(Security.User.Id, activeCustomerId);
                    DashboardCache.InvalidateCache(Security.User.Id, null); // Aggregated view

                    NavigationManager.NavigateTo("/product-search", forceLoad: true);
                }
                else
                {
                    var errorMessage = result?.Metadata?.Message ?? "Sipariş oluşturulurken bir hata oluştu.";
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Sipariş Hatası",
                        Detail = errorMessage,
                        Duration = 5000
                    });
                }
            }
            finally 
            {
                isProcessing = false;
                StateHasChanged();
            }
        }

        private async void OnCartStateChanged()
        {
            try
            {
                CurrentCart = CartStateService.CurrentCart;
                
                // Refresh CalculatedGrandTotal whenever cart state changes
                if (CurrentCart != null)
                {
                    CalculatedGrandTotal = CurrentCart.OrderTotal;
                }
                
                // If cart becomes empty, redirect
                if (CurrentCart == null || CurrentCart.TotalItems == 0)
                {
                    await InvokeAsync(() =>
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Info,
                            Summary = "Sepet Boş",
                            Detail = "Sepetiniz boşaldı.",
                            Duration = 3000
                        });
                        NavigationManager.NavigateTo("/product-search");
                    });
                    return;
                }

                // Clear any stuck updating states when cart changes externally
                if (updatingItems.Count > 0)
                {
                    updatingItems.Clear();
                }
                isUpdatingCart = false;

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                // Log error silently
            }
        }

        private async Task OnSellerCargoChanged(CartSellerDto seller, CartCargoDto cargo)
        {
            try
            {
                seller.SelectedCargo = cargo;
                
                // Update local cart preferences
                if (CartPreferences.SelectedCargoes == null)
                {
                    CartPreferences.SelectedCargoes = new Dictionary<int, int>();
                }
                
                CartPreferences.SelectedCargoes[seller.SellerId] = cargo.CargoId;
                
                // Refresh cart with preferences to get updated totals and persist selection
                await CartStateService.RefreshCart(CartPreferences);
                CurrentCart = CartStateService.CurrentCart;
                
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error changing cargo selection");
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Kargo seçimi güncellenirken bir hata oluştu: " + ex.Message,
                    Duration = 4000
                });
            }
        }

        private string GetEstimatedDelivery()
        {
            // Simple estimation: 1-3 business days
            var deliveryDate = DateTime.Now.AddDays(2);
            return deliveryDate.ToString("dddd", new System.Globalization.CultureInfo("tr-TR"));
        }

        private async Task RemoveItem(int itemId, int? productSellerItemId = null)
        {
            if (isUpdatingCart) return;
            
            try
            {
                isUpdatingCart = true;
                if (productSellerItemId.HasValue)
                {
                    updatingItems.Add(productSellerItemId.Value);
                }
                await InvokeAsync(StateHasChanged);
                
                var result = await CartService.CartItemRemove(itemId);
                
                if (result.Ok && result.Result != null)
                {
                    // Background refresh cart state to sync UI
                    _ = Task.Run(async () => await CartStateService.RefreshCart(CartPreferences));

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Silindi",
                        Detail = result.Metadata?.Message ?? "Ürün sepetten kaldırıldı.",
                        Duration = 2000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = result.Metadata?.Message ?? "Ürün silinemedi.",
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
                isUpdatingCart = false;
                if (productSellerItemId.HasValue)
                {
                    updatingItems.Remove(productSellerItemId.Value);
                }
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task UpdateQuantity(CartItemDto item, int delta)
        {
            var newQty = item.Quantity + delta;
            
            // If quantity would be 0 or less, remove the item
            if (newQty <= 0)
            {
                await RemoveItem(item.Id, item.ProductSellerItemId);
                return;
            }
            
            if (item.MaxSellCount > 0 && newQty > item.MaxSellCount)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Uyarı",
                    Detail = $"Maksimum {item.MaxSellCount} adet satın alabilirsiniz.",
                    Duration = 3000
                });
                return;
            }
            
            if (item.MinSellCount > 0 && newQty < item.MinSellCount)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Uyarı",
                    Detail = $"Minimum {item.MinSellCount} adet satın almalısınız.",
                    Duration = 3000
                });
                return;
            }
            
            var e = new ChangeEventArgs { Value = newQty };
            await OnQuantityChanged(item, e);
        }

        private async Task OnQuantityChanged(CartItemDto item, ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newQty) && newQty > 0)
            {
                if (newQty == item.Quantity) return;
                
                var delta = newQty - item.Quantity;
                if (delta == 0) return;

                try
                {
                    updatingItems.Add(item.ProductSellerItemId);
                    await InvokeAsync(StateHasChanged);

                    var req = new ecommerce.Web.Domain.Dtos.Cart.CartItemUpsertDto 
                    { 
                        ProductSellerItemId = item.ProductSellerItemId, 
                        Quantity = delta 
                    };
                    var result = await CartService.CreateCartItem(req);
                    
                    if (result.Ok && result.Result != null)
                    {
                        // Background refresh cart state to sync UI with preferences
                        _ = Task.Run(async () => await CartStateService.RefreshCart(CartPreferences));

                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Güncellendi",
                            Detail = "Miktar güncellendi.",
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
                    updatingItems.Remove(item.ProductSellerItemId);
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private bool IsItemUpdating(CartItemDto item)
        {
            return updatingItems.Contains(item.ProductSellerItemId);
        }

        private async Task OnSellerCheckboxChanged(int sellerId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            
            if (isUpdatingCart) return;
            
            try
            {
                isUpdatingCart = true;
                await InvokeAsync(StateHasChanged);
                
                var response = await CartService.PassiveCartItemBySellerId(sellerId, isChecked);
                
                if (response.Ok && response.Result != null)
                {
                    // Clear updating state BEFORE setting cart to prevent stuck spinner
                    isUpdatingCart = false;
                    updatingItems.Clear(); // Clear all items since seller affects all items
                    
                    CartStateService.SetCart(response.Result);
                    CurrentCart = CartStateService.CurrentCart;
                    
                    var message = isChecked 
                        ? "Satıcı ürünleri sepete eklendi" 
                        : "Satıcı ürünleri sepetten çıkarıldı";
                    
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Sepet",
                        Detail = response.Metadata?.Message ?? message,
                        Duration = 3000
                    });
                    
                    // Force UI update after cart state change
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Sepet Hatası",
                        Detail = response.Metadata?.Message ?? "Sepet güncellenirken hata oluştu",
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
                // Ensure cleanup even if something goes wrong
                isUpdatingCart = false;
                updatingItems.Clear();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnProductCheckboxChanged(int productSellerItemId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            
            if (isUpdatingCart) return;
            
            try
            {
                isUpdatingCart = true;
                updatingItems.Add(productSellerItemId);
                await InvokeAsync(StateHasChanged);
                
                var response = await CartService.PassiveCartItemByProductSellerItemId(productSellerItemId, isChecked);
                
                if (response.Ok && response.Result != null)
                {
                    // Clear updating state BEFORE setting cart to prevent stuck spinner
                    isUpdatingCart = false;
                    updatingItems.Remove(productSellerItemId);
                    
                    CartStateService.SetCart(response.Result);
                    CurrentCart = CartStateService.CurrentCart;
                    
                    var message = isChecked 
                        ? "Ürün sepete eklendi" 
                        : "Ürün sepetten çıkarıldı";
                    
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Sepet",
                        Detail = response.Metadata?.Message ?? message,
                        Duration = 3000
                    });
                    
                    // Force UI update after cart state change
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Sepet Hatası",
                        Detail = response.Metadata?.Message ?? "Sepet güncellenirken hata oluştu",
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
                // Ensure cleanup even if something goes wrong
                isUpdatingCart = false;
                updatingItems.Remove(productSellerItemId);
                await InvokeAsync(StateHasChanged);
            }
        }

        public void Dispose()
        {
            if (CartStateService != null)
            {
                CartStateService.OnChange -= OnCartStateChanged;
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


        private async Task ProcessPayment()
        {
            try
            {
                var activeCustomerId = Security.SelectedCustomerId ?? Security.User?.CustomerId;
                
                var checkoutRequest = new CheckoutRequestDto
                {
                    UserAddressId = SelectedUserAddressId,
                    CardPayment = PaymentModel,
                    PlatformType = OrderPlatformType.B2B,
                    OnBehalfOfCustomerId = activeCustomerId
                };

                var result = await CheckoutService.Checkout(checkoutRequest);
                
                if (result != null && result.Ok && result.Result != null)
                {
                    if (!string.IsNullOrEmpty(result.Result.CheckoutFormContent))
                    {
                        // 3D Secure payment - Use the manual modal for higher reliability
                        PaymentModalService.Show(result.Result.CheckoutFormContent);
                        await InvokeAsync(StateHasChanged);

                        try
                        {
                            // Wait a bit more for the dialog to settle and Blazor to render the MarkupString
                            await Task.Delay(300);
                            await JSRuntime.InvokeVoidAsync("window.checkoutHelpers.submitForm");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("JS Submit Error: " + ex.Message);
                        }
                    }
                    else
                    {
                        // Direct success
                        var orderNumbers = result.Result.OrderNumbers != null && result.Result.OrderNumbers.Any() 
                            ? string.Join(", ", result.Result.OrderNumbers) 
                            : "N/A";
                        
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Sipariş Oluşturuldu",
                            Detail = $"Siparişiniz başarıyla oluşturuldu. Sipariş No: {orderNumbers}",
                            Duration = 5000
                        });

                        _ = Task.Run(async () => await CartStateService.RefreshCart());
                        NavigationManager.NavigateTo("/", forceLoad: true);
                    }
                }
                else
                {
                    var errorMessage = result?.Metadata?.Message ?? "Ödeme işlemi sırasında bir hata oluştu.";
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Ödeme Başarısız",
                        Detail = errorMessage,
                        Duration = 5000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Ödeme işlemi sırasında bir hata oluştu.",
                    Duration = 5000
                });
            }
            finally
            {
                isProcessing = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task ClosePaymentModal()
        {
            PaymentModalService.Close();
            DialogService.Close();
            await CheckoutService.OrderDelete();
            await InvokeAsync(StateHasChanged);
        }

        [Microsoft.JSInterop.JSInvokable]
        public async Task ProcessPaymentResult(string status, string messageOrOrderNumber)
        {
            PaymentModalService.Close();
            DialogService.Close();
            await InvokeAsync(StateHasChanged);

            if (status == "success")
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Ödeme Başarılı",
                    Detail = $"Siparişiniz başarıyla alındı. Sipariş No: {messageOrOrderNumber}",
                    Duration = 5000
                });
                
                // Invalidate dashboard cache
                var activeCustomerId = Security.SelectedCustomerId ?? Security.User?.CustomerId;
                DashboardCache.InvalidateCache(Security.User.Id, activeCustomerId);
                DashboardCache.InvalidateCache(Security.User.Id, null); 

                // Clear cart state
                await CartStateService.RefreshCart(CartPreferences);
        
        // Redirect to Product Search
        NavigationManager.NavigateTo("/product-search", forceLoad: true);
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Ödeme Başarısız",
                    Detail = messageOrOrderNumber,
                    Duration = 5000
                });
            }
        }

    }
}
