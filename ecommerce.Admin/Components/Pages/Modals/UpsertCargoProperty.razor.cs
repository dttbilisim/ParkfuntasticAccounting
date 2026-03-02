using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CargoPropertyDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertCargoProperty
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
        public ICargoService CargoService { get; set; }

        [Inject]
        public ICargoPropertyService CargoPropertyService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        [Parameter]
        public int? Id { get; set; }

        [Parameter]
        public int CargoId { get; set; }


        protected bool errorVisible;
        protected CargoPropertyUpsertDto cargoProperty = new();

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var response = await CargoPropertyService.GetCargoPropertyById(Id.Value);
                if (response.Ok)
                    cargoProperty = response.Result;
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }


        protected async Task FormSubmit()
        {
            try
            {
                cargoProperty.CargoId = CargoId;
                var submitRs = await CargoPropertyService.UpsertCargoProperty(new Core.Helpers.AuditWrapDto<CargoPropertyUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = cargoProperty
                });
                if (submitRs.Ok)
                {
                    DialogService.Close(cargoProperty);
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
    }
}
