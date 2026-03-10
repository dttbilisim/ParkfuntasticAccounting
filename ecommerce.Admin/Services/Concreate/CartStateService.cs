using ecommerce.Admin.Services.Interfaces;

namespace ecommerce.Admin.Services.Concreate
{
    public class CartStateService : ICartStateService
    {
        private readonly ecommerce.Web.Domain.Services.Abstract.ICartService _cartService;
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        public CartStateService(ecommerce.Web.Domain.Services.Abstract.ICartService cartService)
        {
            _cartService = cartService;
        }

        public event Action? OnChange;

        public ecommerce.Web.Domain.Dtos.Cart.CartDto? CurrentCart { get; private set; }

        public async Task RefreshCart(ecommerce.Core.Entities.CartCustomerSavedPreferences? preferences = null)
        {
            await _refreshLock.WaitAsync();
            try
            {
                var result = await _cartService.GetCart(preferences);
                if (result.Ok && result.Result != null)
                {
                    CurrentCart = result.Result;
                    NotifyStateChanged();
                }
            }
            finally
            {
                _refreshLock.Release();
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
