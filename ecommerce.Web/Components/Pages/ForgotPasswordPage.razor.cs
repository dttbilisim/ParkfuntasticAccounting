using Blazored.LocalStorage;
using ecommerce.Core.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Web.Components.Pages;

public partial class ForgotPasswordPage
{
      [Inject] private II18N lang{get;set;}
    [Inject] private ILocalStorageService _localStorage{get;set;}
    [Inject] private IJSRuntime _jsRuntime{get;set;}

    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private IUserManager _userManager { get; set; }
    [Inject] private NotificationService _notificationService { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    private Core.Entities.Authentication.User user = new();
    private UserClaims _userClaims = new();
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
       
            await _jsRuntime.InvokeVoidAsync("feather.replace");
            var localLanguage = await _localStorage.GetItemAsync<string>("lang");
            if (localLanguage != null)
            {
                _appStateManager.InvokeLanguageChanged(localLanguage);
                lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
            }
            StateHasChanged();
        }
       
    }

    private async Task ForgotPassword()
    {
        try
        {

            await _jsRuntime.ShowFullPageLoader();
            var result = await _userManager.ForgotPasswordAsync(user.Email);

            if (result.Ok)
            {
                _notificationService.Notify(NotificationSeverity.Success, @lang["NotifactionSucces"], result.Result);
                user = new();
            }
            else
            {
                _notificationService.Notify(NotificationSeverity.Error, @lang["NotifactionFailed"]);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await _jsRuntime.HideFullPageLoader();
        }
    }
    
    
}