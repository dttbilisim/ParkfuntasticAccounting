using ecommerce.Admin.Services.Interfaces;

namespace ecommerce.Admin.Services.Concreate
{
    public class CartStateService : ICartStateService
    {
        private readonly ecommerce.Web.Domain.Services.Abstract.ICartService _cartService;

        public CartStateService(ecommerce.Web.Domain.Services.Abstract.ICartService cartService)
        {
            _cartService = cartService;
        }

        public event Action? OnChange;

        public ecommerce.Web.Domain.Dtos.Cart.CartDto? CurrentCart { get; private set; }

        public async Task RefreshCart(ecommerce.Core.Entities.CartCustomerSavedPreferences? preferences = null)
        {
            var result = await _cartService.GetCart(preferences);
            if (result.Ok && result.Result != null)
            {
                CurrentCart = result.Result;
                NotifyStateChanged();
            }
        }

        public void SetCart(ecommerce.Web.Domain.Dtos.Cart.CartDto cart)
        {
            CurrentCart = cart;
            NotifyStateChanged();
        }

        public void NotifyStateChanged() => OnChange?.Invoke();
    }
}
