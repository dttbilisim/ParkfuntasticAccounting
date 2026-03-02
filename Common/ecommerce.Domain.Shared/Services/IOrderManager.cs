using ecommerce.Core.Entities;

namespace ecommerce.Domain.Shared.Services;

public interface IOrderManager
{
    Task<List<CartItem>> GetShoppingCart();

    Task ClearShoppingCart(int companyId, int? sellerId = null);

    Task<CartResult> CalculateShoppingCart(List<CartItem> cart, CartCustomerSavedPreferences? cartPreferences = null, bool includeCommissions = false);

    Task<List<Discount>> GetAllAllowedDiscountsAsync(List<CartItem> cart, string? couponCode = null);

    Task<bool> ValidateDiscountWithRuleAsync(Discount discount);

    List<Discount> GetPreferredDiscount(IList<Discount> discounts, decimal amount, out decimal discountAmount);

    decimal GetDiscountAmount(Discount discount, decimal amount);

    Task<decimal> GetAppMinimumCartTotal();
}