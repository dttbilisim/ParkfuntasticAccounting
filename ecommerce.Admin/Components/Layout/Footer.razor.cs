using System.Linq;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.EFCore.Context;

namespace ecommerce.Admin.Components.Layout;

public partial class Footer : ComponentBase, IDisposable
{
    [Inject]
    private AuthenticationService Security { get; set; } = null!;

    [Inject]
    private ICartStateService CartStateService { get; set; } = null!;

    [Inject]
    private IOrderService OrderService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private ILogger<Footer> Logger { get; set; } = null!;

    private CartDto? _currentCart;
    private int _pendingOrderCount = 0;
    private decimal _pendingOrderTotal = 0m;

    [Inject]
    private IServiceScopeFactory ScopeFactory { get; set; } = null!;

    private string? _plasiyerName;
    private string? _plasiyerPhone;
    private string? _plasiyerEmail;
    private bool _hasDefaultPlasiyer = false;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to cart changes first
        CartStateService.OnChange += OnCartStateChanged;
        
        // Subscribe to customer changes
        Security.OnSelectedCustomerChanged += OnSelectedCustomerChanged;
        
        // Load initial cart state
        if (CartStateService != null)
        {
            try
            {
                await CartStateService.RefreshCart();
                _currentCart = CartStateService.CurrentCart;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Footer: Failed to load initial cart");
                _currentCart = CartStateService?.CurrentCart;
            }
        }
        
        // Load pending orders
        await LoadPendingOrders();

        // Load Default Plasiyer
        await LoadDefaultPlasiyer();
        
        Logger.LogInformation("Footer: OnInitializedAsync - Event subscribed. OrderTotal: {OrderTotal}, PendingOrders: {PendingCount}", 
            _currentCart?.OrderTotal ?? 0m, _pendingOrderCount);
    }

    private async Task LoadDefaultPlasiyer()
    {
        try
        {
            if (Security.User == null) return;
            
            // Get CustomerId based on logged in user logic (same as OrderService)
            // If the user IS a Customer (ApplicationUser with CustomerId), we use that.
            var customerId = Security.User.CustomerId;
            if (!customerId.HasValue) return;

            using (var scope = ScopeFactory.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ecommerce.EFCore.Context.ApplicationDbContext>>();
                var plasiyer = await uow.DbContext.CustomerPlasiyers
                    .AsNoTracking()
                    .Include(cp => cp.SalesPerson)
                    .FirstOrDefaultAsync(cp => cp.CustomerId == customerId.Value && cp.IsDefault);

                if (plasiyer != null && plasiyer.SalesPerson != null)
                {
                    Logger.LogInformation("Footer: Found Default Plasiyer: {Name}", plasiyer.SalesPerson.FirstName);
                    var sp = plasiyer.SalesPerson;
                    _plasiyerName = $"{sp.FirstName} {sp.LastName}".Trim();
                    _plasiyerPhone = !string.IsNullOrEmpty(sp.MobilePhone) ? sp.MobilePhone : sp.Phone ?? "";
                    _plasiyerEmail = sp.Email ?? "";
                    _hasDefaultPlasiyer = true;
                }
                else 
                {
                    Logger.LogWarning("Footer: No Default Plasiyer found for CustomerId: {CustomerId}", customerId);
                }
                
                // Ensure UI updates
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Footer: Failed to load default plasiyer");
        }
    }

    [Inject]
    private IDashboardCacheService DashboardCacheService { get; set; } = null!;

    private async Task LoadPendingOrders()
    {
        try
        {
            if (Security.User == null) return;

            var dashboardData = await DashboardCacheService.GetDashboardDataAsync(Security.User.Id, Security.SelectedCustomerId);
            
            if (dashboardData != null)
            {
                _pendingOrderCount = dashboardData.PendingOrderCount;
                _pendingOrderTotal = dashboardData.PendingOrderTotal;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Footer: Failed to load pending orders from DashboardCacheService");
        }
    }

    private async void OnCartStateChanged()
    {
        try
        {
            var oldCart = _currentCart;
            
            // Get the latest cart from service
            _currentCart = CartStateService?.CurrentCart;
            
            Logger.LogInformation("Footer: OnCartStateChanged - Event triggered. OrderTotal: {OrderTotal}, ItemCount: {ItemCount}, OldTotal: {OldTotal}", 
                _currentCart?.OrderTotal ?? 0m, 
                _currentCart?.TotalItems ?? 0,
                oldCart?.OrderTotal ?? 0m);
            
            // Reload pending orders when cart changes (order might have been created)
            await LoadPendingOrders();
            
            // Always update UI when event fires (cart might have changed even if reference is same)
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Footer: Error in OnCartStateChanged");
        }
    }

    public int GetCartItemCount()
    {
        // Plasiyer with no customer selected -> Show 0
        if (Security.User?.SalesPersonId.HasValue == true && !Security.SelectedCustomerId.HasValue)
        {
            return 0;
        }

        if (_currentCart == null) return 0;

        // Use CartCount (unique product count) like CartPage.razor does
        return _currentCart.CartCount > 0 ? _currentCart.CartCount : 0;
    }

    public string GetCartTotal()
    {
        // Plasiyer with no customer selected -> Show empty or zero formatted
        if (Security.User?.SalesPersonId.HasValue == true && !Security.SelectedCustomerId.HasValue)
        {
            return 0m.ToString("C");
        }

        var total = _currentCart?.OrderTotal ?? 0m;
        var subTotal = _currentCart?.SubTotal ?? 0m;
        var cargoPrice = _currentCart?.CargoPrice ?? 0m;
        var sellersCount = _currentCart?.Sellers?.Count ?? 0;
        
        Logger.LogWarning("Footer: GetCartTotal - OrderTotal: {OrderTotal}, SubTotal: {SubTotal}, CargoPrice: {CargoPrice}, SellersCount: {SellersCount}", 
            total, subTotal, cargoPrice, sellersCount);
        
        // If OrderTotal is 0 but we have items, calculate manually as fallback
        if (total == 0m && _currentCart?.Sellers?.Any() == true)
        {
            var calculatedTotal = _currentCart.Sellers
                .SelectMany(s => s.Items)
                .Sum(i => i.UnitPrice * i.Quantity);
            
            Logger.LogWarning("Footer: Calculated manual total: {CalculatedTotal}", calculatedTotal);
            
            if (calculatedTotal > 0)
            {
                return calculatedTotal.ToString("C");
            }
        }
        
        return total.ToString("C");
    }

    public int GetPendingOrderCount()
    {
        return _pendingOrderCount;
    }

    public string GetPendingOrderTotal()
    {
        return _pendingOrderTotal.ToString("C");
    }

    private void NavigateToCart()
    {
        NavigationManager.NavigateTo("/admin-checkout");
    }

    private void NavigateToPendingOrders()
    {
        NavigationManager.NavigateTo("/b2b/my-orders");
    }

    private async void OnSelectedCustomerChanged()
    {
        try
        {
            Logger.LogInformation("Footer: OnSelectedCustomerChanged - Refreshing data");
            
            // Refresh cart for the new customer context
            await CartStateService.RefreshCart();
            _currentCart = CartStateService.CurrentCart;
            
            // Refresh pending orders for the new customer context
            await LoadPendingOrders();
            
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Footer: Error in OnSelectedCustomerChanged");
        }
    }

    public void Dispose()
    {
        CartStateService.OnChange -= OnCartStateChanged;
        
        if (Security != null)
        {
            Security.OnSelectedCustomerChanged -= OnSelectedCustomerChanged;
        }
        
        Logger.LogInformation("Footer: Disposed - Event unsubscribed");
    }
}
