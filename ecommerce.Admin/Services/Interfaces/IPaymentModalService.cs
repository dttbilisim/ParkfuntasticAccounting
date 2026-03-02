using Microsoft.AspNetCore.Components;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IPaymentModalService
    {
        bool IsOpen { get; }
        string? PaymentHtmlContent { get; }
        event Action? OnChange;

        void Show(string htmlContent);
        void Close();
    }
}
