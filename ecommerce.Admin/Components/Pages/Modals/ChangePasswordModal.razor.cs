using Microsoft.AspNetCore.Components;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class ChangePasswordModal
    {
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected IIdentityUserService IdentityUserService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;

        protected string currentPassword = "";
        protected string newPassword = "";
        protected string confirmPassword = "";
        protected bool isLoading = false;
        protected bool isSuccess = false;
        protected string? errorMessage = null;

        protected async Task HandleChangePassword()
        {
            try
            {
                errorMessage = null;

                if (newPassword != confirmPassword)
                {
                    errorMessage = "Yeni şifreler eşleşmiyor.";
                    return;
                }

                if (newPassword.Length < 6)
                {
                    errorMessage = "Yeni şifre en az 6 karakter olmalıdır.";
                    return;
                }

                isLoading = true;
                StateHasChanged();

                var userId = Security.User?.Id ?? 0;
                if (userId == 0)
                {
                    errorMessage = "Kullanıcı oturumu bulunamadı.";
                    return;
                }

                var result = await IdentityUserService.ChangePasswordAsync(userId, currentPassword, newPassword);

                if (result.Ok)
                {
                    isSuccess = true;
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Şifreniz başarıyla güncellendi.",
                        Duration = 4000
                    });
                    
                    // Auto close modal after 2 seconds on success
                    await Task.Delay(2000);
                    DialogService.Close(true);
                }
                else
                {
                    errorMessage = result.Metadata?.Message ?? "Şifre değiştirilirken bir hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Bir sistem hatası oluştu: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }
    }
}
