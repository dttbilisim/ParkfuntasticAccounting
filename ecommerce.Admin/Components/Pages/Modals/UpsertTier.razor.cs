using AutoMapper;
using ecommerce.Admin.Domain.Dtos.TierDto;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ecommerce.Core.Utils.ResultSet;
using Radzen;
using Microsoft.AspNetCore.Components.Web;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertTier
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
        public ITierService TierService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        [Parameter]
        public int? Id { get; set; }

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected TierUpsertDto tier = new();
        public bool Status { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var response = await TierService.GetTiersById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    tier = response.Result;
                    Status = tier.Status == (int)EntityStatus.Passive || tier.Status == (int)EntityStatus.Deleted ? false : true;

                    if (tier.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                tier.Id = Id;
                tier.StatusBool = Status;

                var submitRs = await TierService.UpsertTier(new Core.Helpers.AuditWrapDto<TierUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = tier
                });
                if (submitRs.Ok)
                {

                    DialogService.Close(tier);
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
