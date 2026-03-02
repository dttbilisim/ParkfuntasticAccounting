using Microsoft.AspNetCore.Components;

namespace ecommerce.Web.Utility;

public class LanguageComponentBase : ComponentBase
{
    [Inject]
    public AppStateManager AppStateManager { get; set; }
    

    protected override void OnAfterRender(bool firstRender) {
        if (firstRender) {
            AppStateManager.LanguageChanged += (_, lang) => StateHasChanged();
        }
    }
}