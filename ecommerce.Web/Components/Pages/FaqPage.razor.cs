using Blazored.LocalStorage;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ecommerce.Web.Components.Pages;

public partial class FaqPage
{
    private string searchText = string.Empty;
   
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private ILocalStorageService _localStorage { get; set; }
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    [Inject] IFrequentlyAskedQuestionService FaqService { get; set; }
    private List<FrequentlyAskedQuestion>? faqItems;

    private IEnumerable<FrequentlyAskedQuestion> FilteredFaqItems =>
        string.IsNullOrWhiteSpace(searchText)
            ? faqItems ?? Enumerable.Empty<FrequentlyAskedQuestion>()
            : (faqItems ?? Enumerable.Empty<FrequentlyAskedQuestion>())
                .Where(f => (!string.IsNullOrEmpty(f.Name) && f.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(f.Description) && f.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)));

    protected override async Task OnInitializedAsync()
    {
        var rs = await FaqService.GetAllAsync(SSSAndBlogGroup.SSS);
        faqItems = rs.Result ?? new List<FrequentlyAskedQuestion>();
    }
}