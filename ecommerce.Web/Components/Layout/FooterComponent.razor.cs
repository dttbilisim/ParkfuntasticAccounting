using Blazored.LocalStorage;
using ecommerce.Domain.Shared.Dtos.Category;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ecommerce.Web.Components.Layout;

public partial class FooterComponent
{
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private ILocalStorageService _localStorage { get; set; }
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    [Inject] ICategoryService _categoryService { get; set; }
    
    private List<CategoryElasticDto> categories;

    protected override async Task  OnInitializedAsync()
    {
        
        var result = await _categoryService.GetAllCategoryFooter();
        if (result != null)
        {
            categories = result.Result;
        }
        
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        { 
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