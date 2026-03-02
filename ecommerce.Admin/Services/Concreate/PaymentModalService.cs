using ecommerce.Admin.Services.Interfaces;

namespace ecommerce.Admin.Services.Concreate
{
    public class PaymentModalService : IPaymentModalService
    {
        public bool IsOpen { get; private set; }
        public string? PaymentHtmlContent { get; private set; }
        public event Action? OnChange;

        public void Show(string htmlContent)
        {
            IsOpen = true;
            PaymentHtmlContent = htmlContent;
            NotifyStateChanged();
        }

        public void Close()
        {
            IsOpen = false;
            PaymentHtmlContent = null;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
