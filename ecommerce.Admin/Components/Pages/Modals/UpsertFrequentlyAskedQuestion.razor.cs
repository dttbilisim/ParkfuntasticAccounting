using ecommerce.Admin.Domain.Dtos.FrequentlyAskedQuestionsDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Radzen;
using Microsoft.AspNetCore.Components.Web;
using ecommerce.Admin.CustomComponents.Modals;
using static ecommerce.Admin.ConfigureValidators.Validations;
using Blazored.FluentValidation;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertFrequentlyAskedQuestion
    {
        #region Injection

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Inject]
        public IFrequentlyAskedQuestionService Service { get; set; }

        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        #endregion

        private bool errorVisible;
        private FrequentlyAskedQuestionUpsertDto sss = new();
        public List<string> ValidationErrors = new();
        private List<FrequentlyAskedQuestionListDto> sssList= new();
        private FluentValidationValidator? _fluentValidationValidator;

        IEnumerable<SSSAndBlogGroup> groups = Enum.GetValues(typeof(SSSAndBlogGroup)).Cast<SSSAndBlogGroup>();

        protected override async Task OnInitializedAsync()
        {
            sssList.Add(new FrequentlyAskedQuestionListDto() { Name="Ana Kategori"});
            var sssResponse = await Service.GetFrequentlyAskedQuestions();
            if (sssResponse.Ok)
                sssList.AddRange(sssResponse.Result?.ToList());

            if (Id.HasValue)
            {
                var response = await Service.GetFrequentlyAskedQuestionById(Id.Value);
                if (response.Ok && response.Result!=null)
                {
                    sss = response.Result;
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                sss.Id = Id;
                var submitRs = await Service.UpsertFrequentlyAskedQuestion(new Core.Helpers.AuditWrapDto<FrequentlyAskedQuestionUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = sss
                });
                if (submitRs.Ok)
                {

                    DialogService.Close(sss);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }

        protected async Task ShowErrors()
        {
            var validator = new FAQBlogUpsertValidator();
            var res = validator.Validate(sss);


            ValidationErrors.AddRange(res.Errors.Select(x => x.ErrorMessage));
            List<Dictionary<string, string>> error = await PrepareErrorsForWarningModal(ValidationErrors);




            Dictionary<string, object> param = new();
            param.Add("Errors", error);
            await DialogService.OpenAsync<ValidationModal>("Uyari", param);

            ValidationErrors.Clear();
        }

        private async Task<List<Dictionary<string, string>>> PrepareErrorsForWarningModal(List<string> errors)
        {
            List<Dictionary<string, string>> error = new();
            foreach (var errorText in ValidationErrors)
            {
                Dictionary<string, string> messageDictionary = new Dictionary<string, string>();
                messageDictionary.Add(errorText.Split("-")[0], errorText.Split("-")[1]);
                error.Add(messageDictionary);
            }
            return error;
        }
    }
}
