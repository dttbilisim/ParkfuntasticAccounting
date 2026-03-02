using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils.Threading;
using ecommerce.Domain.Shared.Services;
using Newtonsoft.Json;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class CartTotalValueProvider : FieldDefinitionValueProvider
{
    private readonly CurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public CartTotalValueProvider(
        CurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public override async Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return 0;
        }

        var httpContext = _httpContextAccessor.HttpContext;

        var lockKey = nameof(CartTotalValueProvider) + _currentUser.GetUserId() + httpContext?.TraceIdentifier;

        if (KeyedLock.IsLockHeld(lockKey))
        {
            return 0;
        }

        // We must prevent the rule from indirectly calling itself. It would cause a stack overflow on cart page.
        // CalculateShoppingCart method calls validate discounts with this rule engine.
        using (await KeyedLock.LockAsync(lockKey))
        {
            var orderManager = _serviceProvider.GetRequiredService<IOrderManager>();
            var cart = await orderManager.GetShoppingCart();

            CartCustomerSavedPreferences? cartPreferences = null;

            try
            {
                cartPreferences = JsonConvert.DeserializeObject<CartCustomerSavedPreferences>(
                    httpContext?.Request.Cookies["Cart"] ?? string.Empty
                );
            }
            catch
            {
                // ignored
            }

            var calculateResult = await orderManager.CalculateShoppingCart(cart, cartPreferences);

            return Math.Round(calculateResult.OrderTotal, 2);
        }
    }
}