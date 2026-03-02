using System.Text.RegularExpressions;
using Blazored.LocalStorage;
using ecommerce.Core.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Web.Components.Pages;

public partial class ResetPasswordPage
{
    [Inject] private ILocalStorageService _localStorageService { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private IUserManager _userManager { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private NotificationService NotificationService { get; set; }
    [Inject] private IJSRuntime JS { get; set; }

    [Parameter] [SupplyParameterFromQuery] public string? token { get; set; }
    [Parameter] [SupplyParameterFromQuery] public string? email { get; set; }

    private UserClaims _userClaims = new();

    private string NewPassword { get; set; }
    private string ConfirmPassword { get; set; }
    private bool IsProcessing = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
         

                var localLanguage = await _localStorageService.GetItemAsync<string>("lang");
                if (localLanguage != null)
                {
                    _appStateManager.InvokeLanguageChanged(localLanguage);
                    lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
                }

                StateHasChanged();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    private async Task OnResetPassword()
    {
        try
        {
            await JS.ShowFullPageLoader();
            if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                NotificationService.Notify(NotificationSeverity.Warning, @lang["NotNull"]);
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                NotificationService.Notify(NotificationSeverity.Warning, @lang["PasswordsDoNotMatch"]);
                return;
            }

            if (!Regex.IsMatch(NewPassword, @"[A-Z]"))
            {
                NotificationService.Notify(NotificationSeverity.Warning, @lang["PasswordMustContainUppercase"]);
                return;
            }


            var user = await _userManager.GetUserByEmailAsync(email);
            if (user == null || user.ResetEmailToken != token || user.ResetEmailTokenExpireDate < DateTime.UtcNow)
            {
                NotificationService.Notify(NotificationSeverity.Error, @lang["ActiveFailed2"]);
                IsProcessing = false;
                return;
            }

            var result = await _userManager.ResetUserPasswordAsync(user, NewPassword);
            if (result.Succeeded)
            {
                user.ResetEmailToken = null;
                user.ResetEmailTokenExpireDate = null;
                await _userManager.UpdateUserAsync(user);

                NotificationService.Notify(NotificationSeverity.Success, @lang["PasswordChanged"]);

                await Task.Delay(2000);

                NavigationManager.NavigateTo("/login", true);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, @lang["PasswordError"]);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }
}