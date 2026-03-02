using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ScaleUnitDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components.Web;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertScaleUnit
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
        public IScaleUnitService Service { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        [Parameter]
        public int? Id { get; set; }

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected ScaleUnitUpsertDto scaleUnit = new();
        public bool Status { get; set; } = true;


        protected override async Task OnInitializedAsync()
        {

            if (Id.HasValue)
            {
                var response = await Service.GetScaleUnitById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    scaleUnit = response.Result;
                    Status = scaleUnit.Status == (int)EntityStatus.Passive || scaleUnit.Status == (int)EntityStatus.Deleted ? false : true;

                    if (scaleUnit.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
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
                scaleUnit.Id = Id;
                scaleUnit.StatusBool = Status;

                var submitRs = await Service.UpsertScaleUnit(new Core.Helpers.AuditWrapDto<ScaleUnitUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = scaleUnit
                });
                if (submitRs.Ok)
                {

                    DialogService.Close(scaleUnit);
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
