using AutoMapper;
using ecommerce.Admin.Domain.Dtos.RegionDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ecommerce.Core.Utils.ResultSet;
using Radzen;
using Microsoft.AspNetCore.Components.Web;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertRegion
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
        public IRegionService RegionService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        [Parameter]
        public int? Id { get; set; }

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected RegionUpsertDto region = new();
        public bool Status { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {

            if (Id.HasValue)
            {
                var response = await RegionService.GetRegionById(Id.Value);
                if (response.Ok)
                {
                    region = response.Result;
                    Status = region.Status == (int)EntityStatus.Passive || region.Status == (int)EntityStatus.Deleted ? false : true;

                    if (region.Status == EntityStatus.Deleted.GetHashCode())
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
                region.Id = Id;
                region.StatusBool = Status;

                var submitRs = await RegionService.UpsertRegion(new Core.Helpers.AuditWrapDto<RegionUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = region
                });
                if (submitRs.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Bölge kaydedildi."
                    });
                    DialogService.Close(region);
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

