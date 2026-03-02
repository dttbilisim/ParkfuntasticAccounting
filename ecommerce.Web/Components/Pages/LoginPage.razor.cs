using System.Text.Json;
using Blazored.LocalStorage;
using ecommerce.Core.Dtos;
using ecommerce.Core.Dtos.Login;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using System.Net.Http.Json;

namespace ecommerce.Web.Components.Pages;

public partial class LoginPage
{
    private class LoginResult { public bool ok { get; set; } public string message { get; set; } }
      [Inject] private ILocalStorageService _localStorageService { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private IJSRuntime _jsruntime { get; set; }
    [Inject] private IHttpClientFactory _httpClientFactory { get; set; }

    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private ICookieManager _cookieManager { get; set; }
    [Inject] NotificationService _notificationService { get; set; }
    private LoginModelRequestDto loginModel = new();
    private UserClaims _userClaims = new();
    private bool showPassword = false;

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

    private async Task LoginAsync()
    {
        await _appStateManager.ExecuteWithLoading(async () => {
            try
            {
                var rs = await _jsruntime.InvokeAsync<LoginResult>("authLogin", loginModel.Email, loginModel.Password);
                if (rs != null && rs.ok)
                {
                    try { await _appStateManager.UpdatedCart(this, null); } catch { }
                    // Tam sayfa yenileme ile header auth state'i kesin güncellensin
                    NavigationManager.NavigateTo("/", forceLoad: true);
                    return;
                }
                else
                {
                    string msg = rs?.message ?? "Giriş başarısız";
                    _notificationService.Notify(NotificationSeverity.Error, "Giriş Hatası", msg);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }, "Giriş yapılıyor");
    }
    
}