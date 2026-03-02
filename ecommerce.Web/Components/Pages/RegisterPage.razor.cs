using Blazored.LocalStorage;
using ecommerce.Core.Dtos;
using ecommerce.Core.Utils;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Web.Components.Pages;

public partial class RegisterPage
{
      [Inject] private ILocalStorageService _localStorageService { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private IJSRuntime _jsruntime { get; set; }
    [Inject] private IUserManager _userService { get; set; }
    [Inject] private NotificationService _notificationService { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    [Inject] private DialogService _dialogService { get; set; }
    private Core.Entities.Authentication.User _user = new();
    private WebUserType SelectedType = WebUserType.B2C;
    private UserClaims _userClaims = new();
    private bool showPassword = false;
    protected override async Task OnInitializedAsync()
    {
        SelectedType = WebUserType.B2C;
        _user.WebUserType = SelectedType;
    }

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

    private async Task RegisterUser()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_user.Email) || string.IsNullOrWhiteSpace(_user.PasswordHash))
            {
                _notificationService.Notify(NotificationSeverity.Error, @lang["LoginNullError"]);
                return;
            }


            var existingUser = await _userService.GetUserByEmailAsync(_user.Email);
            if (existingUser != null)
            {
                _notificationService.Notify(NotificationSeverity.Warning, @lang["EmailAlreadyExists"]);
                return;
            }

            if (!_appStateManager.IsValidTurkishPhoneNumber(_user.PhoneNumber))
            {
                _notificationService.Notify(NotificationSeverity.Warning, @lang["UsersPhoneReq"]);
                return;
            }

            if (!_user.PasswordHash.Any(char.IsUpper))
            {
                _notificationService.Notify(NotificationSeverity.Error, lang["PasswordMustContainUppercase"]);
                return;
            }

            var result = await _userService.CreateUserAsync(_user);
            if (result.Ok)
            {
                if (_user.WebUserType == WebUserType.B2B)
                {
                    _user = new();
                    await _dialogService.Alert(lang["RegisterPendingApproval"], "Bilgi", new AlertOptions()
                    {
                        OkButtonText = lang["Yes"]
                    });
                    _navigationManager.NavigateTo("/login");
                }
                else
                {
                    _user = new();
                    await _dialogService.Alert(lang["RegisterSuccess"], "Bilgi", new AlertOptions()
                    {
                        OkButtonText = lang["Yes"]
                    });
                    _navigationManager.NavigateTo("/login");
                }
            }
            else
            {
                _notificationService.Notify(NotificationSeverity.Error, @lang["RegisterFailed"]);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
    }

    private void SelectUserType(WebUserType type)
    {
        SelectedType = type;
        _user.WebUserType = type;
    }
}