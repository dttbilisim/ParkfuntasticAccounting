using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Admin;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace ecommerce.Admin.Components.Layout
{
    public partial class MainLayout
    {
        #region Injections

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }
        
        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject] 
        protected IMenuService MenuService { get; set; }
        
        [Inject]
        protected AuthenticationService Security { get; set; }
        
        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        
        [Inject]
        protected ecommerce.Web.Domain.Services.Abstract.ICartService CartService { get; set; }
        
        [Inject]
        protected ecommerce.Admin.Services.Interfaces.ICartStateService CartStateService { get; set; }

        [Inject]
        protected IPaymentModalService PaymentModalService { get; set; }

        #endregion

        private bool sidebarExpanded = true;
        private List<Menu> menus = new();
        private string? userName;
        private int cartCount = 0;
        private bool isCartVisible = false;
        private bool isFooterVisible = false;
        protected bool isCartDrawerOpen { get; set; } = false;
        private bool isCartLoading = false;
        private int CopyRightYear = DateTime.Now.Year;

        private IDisposable? _pageUrlLogScope;

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += HandleLocationChanged;
            UpdatePageUrlLogScope(NavigationManager.Uri);

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            await Security.InitializeAsync(authState);

            userName = Security.User.FullName;
            
            // Check if cart should be visible (Plasiyer or CustomerB2B roles)
            isCartVisible = IsCartRoleAllowed();
            isFooterVisible = isCartVisible;
            
            // Subscribe to cart state changes
            if (isCartVisible && CartStateService != null)
            {
                CartStateService.OnChange += CartStateChanged;
                
                // Load cart count from state if available
                if (CartStateService.CurrentCart != null)
                {
                    cartCount = CalculateCartCount(CartStateService.CurrentCart);
                }
            }

            // Subscribe to Payment Modal changes
            PaymentModalService.OnChange += HandleModalStateChanged;

            // Subscribe to customer changes
            Security.OnSelectedCustomerChanged += HandleSelectedCustomerChanged;

            await base.OnInitializedAsync();
        }

        private async void HandleSelectedCustomerChanged()
        {
            await InvokeAsync(async () => {
                await LoadCartInBackground();
            });
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Hide loader
                try 
                {
                   await JSRuntime.InvokeVoidAsync("hideAppLoader");
                }
                catch { }
                
                // Load menu
                try
                {
                    var menuResult = await MenuService.GetMenusForCurrentUser();
                    if (menuResult.Ok && menuResult.Result != null)
                    {
                        menus = menuResult.Result;
                    }
                }
                catch { }
                
                // Update UI (menu loaded)
                StateHasChanged();
                
                // Load cart after menu - proper async, no fire-and-forget
                if (isCartVisible)
                {
                    await LoadCartInBackground();
                }
            }
            
            await base.OnAfterRenderAsync(firstRender);
        }
        
        /// <summary>
        /// Loads cart data - called after first render
        /// </summary>
        private async Task LoadCartInBackground()
        {
            try
            {
                await CartStateService.RefreshCart();
                var cart = CartStateService.CurrentCart;
                if (cart != null)
                {
                    cartCount = CalculateCartCount(cart);
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                // Log error silently
            }
        }
        
    private async void CartStateChanged()
    {
        // Only update local state, do NOT call RefreshCart() here as it causes a loop
         var currentCart = CartStateService.CurrentCart;
         if (currentCart != null)
         {
             cartCount = CalculateCartCount(currentCart);
             await InvokeAsync(StateHasChanged);
         }
    }

    private int CalculateCartCount(ecommerce.Web.Domain.Dtos.Cart.CartDto cart)
    {
        // Plasiyer with no customer selected -> Show 0
        if (Security.User?.SalesPersonId.HasValue == true && !Security.SelectedCustomerId.HasValue)
        {
            return 0;
        }

        if (cart == null)
            return 0;
        
        // Use CartCount (unique product count) like CartPage.razor does
        return cart.CartCount > 0 ? cart.CartCount : 0;
    }
        
        private bool IsCartRoleAllowed()
        {
            if (Security?.User?.Roles == null) return false;
            return Security.User.Roles.Any(r => r.Name == "Plasiyer" || r.Name == "CustomerB2B");
        }
        
        private async Task LoadCartCount()
        {
            if (!isCartVisible) return;
            
            try
            {
                await CartStateService.RefreshCart();
                var cart = CartStateService.CurrentCart;
                
                if (cart != null)
                {
                    cartCount = CalculateCartCount(cart);
                }
                else
                {
                    cartCount = 0;
                }
            }
            catch (Exception ex)
            {
                cartCount = 0;
            }
        }

        private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            UpdatePageUrlLogScope(e.Location);
        }

        private void UpdatePageUrlLogScope(string url)
        {
            _pageUrlLogScope?.Dispose();
            // Push PageUrl to LogContext. Note: In Blazor Server/SignalR, AsyncLocal flow is complex.
            // This might effectively set it for the Circuit's explicit tasks initiated *after* this point 
            // but scope management is tricky. We'll try this as best-effort.
            _pageUrlLogScope = Serilog.Context.LogContext.PushProperty("PageUrl", url);
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= HandleLocationChanged;
            
            // Unsubscribe from cart state changes
            if (CartStateService != null)
            {
                CartStateService.OnChange -= CartStateChanged;
            }
            
            // Unsubscribe from customer changes
            if (Security != null)
            {
                Security.OnSelectedCustomerChanged -= HandleSelectedCustomerChanged;
            }

            _pageUrlLogScope?.Dispose();

            // Unsubscribe from Payment Modal changes
            if (PaymentModalService != null)
            {
                PaymentModalService.OnChange -= HandleModalStateChanged;
            }
        }

        private void HandleModalStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        void SidebarToggleClick()
        {
            sidebarExpanded = !sidebarExpanded;
        }

        protected async void ProfileMenuClick(RadzenProfileMenuItem args)
        {
            if (args.Value == "Logout")
            {
                NavigationManager.NavigateTo("Account/Logout", true);
            }
            else if (args.Value == "Password")
            {
                await DialogService.OpenAsync<ecommerce.Admin.Components.Pages.Modals.ChangePasswordModal>("", 
                    null, 
                    new DialogOptions() { Width = "450px", Height = "auto", CloseDialogOnOverlayClick = true, ShowTitle = false });
            }
        }
        
        private async Task OpenCartDrawer()
        {
            // Toggle drawer immediately
            isCartDrawerOpen = !isCartDrawerOpen;
            
            // If opening, use cached data first then refresh
            if (isCartDrawerOpen)
            {
                // Set loading state
                isCartLoading = true;
                StateHasChanged();
                
                // Show cached data immediately
                    var cart = CartStateService.CurrentCart;
                    if (cart != null)
                    {
                        cartCount = CalculateCartCount(cart);
                    }
           
                try
                {
                    await CartStateService.RefreshCart();
                    cart = CartStateService.CurrentCart;
                    if (cart != null)
                    {
                        cartCount = CalculateCartCount(cart);
                    }
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
            else
            {
                StateHasChanged();
            }
        }
    }
}
