
namespace ecommerce.Admin.Services.Interfaces
{
    public interface ICartStateService
    {
        event Action OnChange;
        
        ecommerce.Web.Domain.Dtos.Cart.CartDto? CurrentCart { get; }
        
        Task RefreshCart(ecommerce.Core.Entities.CartCustomerSavedPreferences? preferences = null);
        
        void SetCart(ecommerce.Web.Domain.Dtos.Cart.CartDto cart);

        void NotifyStateChanged();
    }
}
