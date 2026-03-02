using Blazored.LocalStorage;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Web.Domain.Email;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Domain.Shared.Dtos.SupportLine;
using ecommerce.Web.Domain.Services.Abstract;

namespace ecommerce.Web.Components.Pages;

public partial class ContactPage
{
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private ILocalStorageService _localStorage { get; set; }
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    [Inject]
    private IEmailTemplateService _emailTemplateService { get; set; }
    [Inject] private NotificationService _notificationService { get; set; }
    [Inject] private ICommonManager _commonManager { get; set; }
    
    private SupportLine supportModel = new();

    private List<FrequentlyAskedQuestion> supportQuestion = new();

    protected override async Task OnInitializedAsync()
    {
        var result = await _commonManager.GetFrequentlyAskedQuestions();
        if (result.Ok)
        {
            supportQuestion = result.Result;
        }
    }

    private async Task SubmitSupportLine()
    {
        try
        {
           
            await _jsRuntime.ShowFullPageLoader();
            var rs = await _commonManager.SubmitSupportLineAsync(supportModel);
            if (rs.Ok && rs.Result)
            {
                _notificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = lang["Contact.MessageSent"],
                    Detail = lang["Contact.MessageSentDetail"],
                    Duration = 4000
                });
            }
            else
            {
                _notificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = lang["Contact.MessageFailed"],
                    Detail =  lang["Contact.MessageFailedDetail"],
                    Duration = 5000
                });
            }
            await _jsRuntime.HideFullPageLoader();
        }
        catch (Exception ex)
        {
            await _jsRuntime.HideFullPageLoader();
            _notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = lang["Contact.MessageFailed"],
                Detail = ex.Message,
                Duration = 5000
            });
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

    private void OnFaqChanged()
    {
        var selectedFaq = supportQuestion
            .FirstOrDefault(x => x.Id == supportModel.FrequentlyAskedQuestionsId);

        if (selectedFaq != null)
        {
            supportModel.FrequentlyAskedQuestionsName = selectedFaq.Name;
        }
    }
}