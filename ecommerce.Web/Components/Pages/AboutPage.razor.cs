using Blazored.LocalStorage;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Services.Concreate;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ecommerce.Web.Components.Pages;

public partial class AboutPage
{
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private ILocalStorageService _localStorage { get; set; }
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        { 
            await _jsRuntime.InvokeVoidAsync("sliderThree");
            StateHasChanged();
            var localLanguage = await _localStorage.GetItemAsync<string>("lang");
            if (localLanguage != null)
            {
                _appStateManager.InvokeLanguageChanged(localLanguage);
                lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
            }

            StateHasChanged();
        }
    }

    
}