using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ActiveArticleDto;
using ecommerce.Admin.Domain.Dtos.ProductActiveArcticleDto;
using ecommerce.Admin.Domain.Dtos.ScaleUnitDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Admin.Services;
using Microsoft.AspNetCore.Components.Web;
using Blazored.FluentValidation;
using ecommerce.Admin.CustomComponents.Modals;
using static ecommerce.Admin.ConfigureValidators.Validations;
namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertProductActiveArticle
    {

        #region Injections

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

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
        public IProductActiveArticleService ProductActiveArticleService { get; set; }

        [Inject]
        public IScaleUnitService ScaleUnitService { get; set; }

        [Inject]
        public IActiveArticlesService ActiveArticlesService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion


        [Parameter]
        public int? Id { get; set; }

        [Parameter]
        public int ProductId { get; set; }

        public List<string> ValidationErrors = new();
        protected bool errorVisible;
        private FluentValidationValidator? _fluentValidationValidator;
        protected ProductActiveArticleUpsertDto productActiveArticleUpsertDto = new();
        protected List<ActiveArticleListDto> activeArticles = new();
        protected List<ScaleUnitListDto> scaleUnits = new();
        
        IEnumerable<ScaleType> scaleTypes = Enum.GetValues(typeof(ScaleType)).Cast<ScaleType>();


        protected override async Task OnInitializedAsync()
        {
       

            var activeArticleResponse = await ActiveArticlesService.GetActiveArticles();
            if (activeArticleResponse.Ok)
                activeArticles = activeArticleResponse.Result?.ToList();

            var scaleUnitResponse = await ScaleUnitService.GetScaleUnits();
            if (scaleUnitResponse.Ok)
                scaleUnits = scaleUnitResponse.Result?.ToList();

            productActiveArticleUpsertDto.ProductId = ProductId;

            if (Id.HasValue)
            {
                var response = await ProductActiveArticleService.GetProductActiveArticleById(Id.Value);
                if (response.Ok)
                {
                    productActiveArticleUpsertDto = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                productActiveArticleUpsertDto.Id = Id;
                productActiveArticleUpsertDto.ProductId = ProductId;

                var submitRs = await ProductActiveArticleService.UpsertProductActiveArticle(new Core.Helpers.AuditWrapDto<ProductActiveArticleUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = productActiveArticleUpsertDto
                });
                if (submitRs.Ok)
                {
                    DialogService.Close(productActiveArticleUpsertDto);                    
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

       

        protected async Task ShowErrors()
        {

     

            var validator = new ProductActiveArticleUpsertValidator();
            var res = validator.Validate(productActiveArticleUpsertDto);

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

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }
    }
}
