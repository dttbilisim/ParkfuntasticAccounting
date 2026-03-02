using AutoMapper;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.CargoDto;
using ecommerce.Admin.Domain.Dtos.CargoPropertyDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using static ecommerce.Admin.ConfigureValidators.Validations;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertCargo
    {
        #region Injection

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
        public ICargoService Service { get; set; }

        [Inject]
        public ICargoPropertyService CargoPropertyService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        [Parameter]
        public int? Id { get; set; }

        private bool IsSaveButtonDisabled = false;
        private bool IsShowButton = false;
        protected bool IsCargoSaved=true;
        public List<string> ValidationErrors = new();

        /// <summary>Option item for RadzenDropDown; value tuples do not expose Value/Text as properties.</summary>
        private sealed class CargoTypeOption
        {
            public string Text { get; set; } = "";
            public CargoType Value { get; set; }
        }

        private static readonly List<CargoTypeOption> cargoTypeOptions = new()
        {
            new CargoTypeOption { Text = "Standart Kargo", Value = CargoType.Standard },
            new CargoTypeOption { Text = "Hızlı Kargo Bicops Express", Value = CargoType.BicopsExpress },
        };

        protected bool errorVisible;
        protected CargoUpsertDto cargo = new();
        protected List<CargoPropertyListDto> cargoProperties = null;
        protected DialogOptions DialogOptions = new() { Width="1200px" };

        protected override async Task OnInitializedAsync()
        {

            if (Id.HasValue)
            {
                await LoadCargoProperties(Id.Value);

                var response = await Service.GetCargoById(Id.Value);
                if (response.Ok)
                {
                    cargo = response.Result;
                    IsShowButton=true;
                    IsCargoSaved=false;
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
                cargo.Id = Id;

                var submitRs = await Service.UpsertCargo(new Core.Helpers.AuditWrapDto<CargoUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = cargo
                });
                if (submitRs.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, submitRs.GetMetadataMessages());

                    if (Id.HasValue) 
                    {
                        DialogService.Close(cargo);
                    }
                    else
                    {
                        Id = submitRs.Result;
                        IsShowButton = true;
                        IsCargoSaved = false;
                    }
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

        protected async Task GridDeleteCargoPropertyButtonClick(MouseEventArgs args, CargoPropertyListDto cargoProperty)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen kayıdı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await CargoPropertyService.DeleteCargoProperty(new Core.Helpers.AuditWrapDto<CargoPropertyDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new CargoPropertyDeleteDto() { Id = cargoProperty.Id }
                    });

                    if (deleteResult != null)
                        await LoadCargoProperties(Id.Value);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = $"Error",
                    Detail = $"Unable to delete product"
                });
            }
        }

        private async Task LoadCargoProperties(int cargoId)
        {
            var response = await CargoPropertyService.GetCargoProperties(cargoId);
            if (response.Ok)
                cargoProperties = response.Result;
        }

        protected async Task AddCargoPropertyButtonClick(MouseEventArgs args)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("CargoId", Id.Value);

            await DialogService.OpenAsync<UpsertCargoProperty>("Kargo Özellikleri / Ekle", parameters, DialogOptions);
            await LoadCargoProperties(Id.Value);
        }

        protected async Task EditCargoPropertyButtonClick(CargoPropertyListDto args)
        {
            await DialogService.OpenAsync<UpsertCargoProperty>("Kargo Özellikleri / Düzenle", new Dictionary<string, object> {
                { "Id", args.Id },
                { "CargoId", Id.Value}
            }, DialogOptions);
            await LoadCargoProperties(Id.Value);
        }

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }

        protected async Task ShowErrors()
        {
            var validator = new CargoUpsertValidator();
            var res = validator.Validate(cargo);


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
