using Blazored.LocalStorage;
using ecommerce.Core.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Web.Components.Pages.Components;

public partial class ActiveAccountComponent
{
    [Inject] IUserManager _userManager { get; set; }
    [Inject] NotificationService NotificationService { get; set; }
    [Inject] private ILocalStorageService _localStorageService { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; }
    [Inject] private II18N lang { get; set; }
    private UserClaims _userClaims = new();
    [Parameter]
    [SupplyParameterFromQuery]
    public string? token { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? email { get; set; }

    private bool IsProcessing = true;
    private bool IsSuccess = false;
    private bool IsFailed = false;
    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                IsFailed = true;
                IsProcessing = false;
                return;
            }

            var result = await _userManager.ActivateAccountAsync(token!, email!);
            IsProcessing = false;
            if (!result.Ok)
            {
                IsSuccess = true;
            }
            else
            {
                IsFailed = true;
                NotificationService.Notify(NotificationSeverity.Error, "Aktivasyon başarısız");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                _userClaims = await _appStateManager.GetUserFromCookie();

                if (!string.IsNullOrWhiteSpace(_userClaims.UserId))
                {
                    _navigationManager.NavigateTo("/", forceLoad:true);
                    
                }
                StateHasChanged();
                
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
}