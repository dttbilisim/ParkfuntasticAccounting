using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CompanyCargoDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Admin.Domain.Dtos.CargoDto;
using Microsoft.AspNetCore.Components.Web;
using Blazored.FluentValidation;
using ecommerce.Admin.CustomComponents.Modals;
using static ecommerce.Admin.ConfigureValidators.Validations;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertCompanyCargo
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
        public ICompanyCargoService CompanyCargoService { get; set; }

        [Inject]
        public ICargoService CargoService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        [Parameter]
        public int SellerId { get; set; }

        #endregion

        protected bool errorVisible;
        protected CompanyCargoUpsertDto CompanyCargo = new();
        protected List<CargoListDto> Cargoes = new();
        public List<string> ValidationErrors = new();

        private FluentValidationValidator? _fluentValidationValidator;


        protected override async Task OnInitializedAsync()
        {

            var CargoResponse = await CargoService.GetCargoes();
            if (CargoResponse.Ok)
                Cargoes = CargoResponse.Result;

            if (Id.HasValue)
            {
                var response = await CompanyCargoService.GetCompanyCargoById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    CompanyCargo = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }

        protected async Task FormSubmit()
        {
            CompanyCargo.Id = Id;
            CompanyCargo.SellerId = SellerId;
          


            var submitRs = await CompanyCargoService.UpsertCompanyCargo(new Core.Helpers.AuditWrapDto<CompanyCargoUpsertDto>()
            {
                UserId = Security.User.Id,
                Dto = CompanyCargo
            });
            if (submitRs.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, submitRs.GetMetadataMessages());
                DialogService.Close(null);
            }
            else
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
        }

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }

        protected async Task ShowErrors()
        {
            var validator = new CompanyCargoUpsertDtoDtoValidator();
            var res = validator.Validate(CompanyCargo);


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
