using ecommerce.Admin.Domain.Dtos.ProductTierDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ecommerce.Core.Utils.ResultSet;
using Radzen;
using Microsoft.AspNetCore.Components.Web;
using ecommerce.Admin.Domain.Dtos.TierDto;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertProductTier
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
        public IProductTierService Service { get; set; }

        [Inject]
        public ITierService TierService { get; set; }

        [Inject]
        public IConfiguration Configuration { get; set; }

        [Inject]
        public IAppSettingService AppSettingService { get; set; }


        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        [Parameter]
        public int ProductId { get; set; }

        #endregion

        protected bool errorVisible;
        private bool IsSaveButtonDisabled = false;
        protected ProductTierUpsertDto productTier = new();
        protected List<TierListDto> tiers = new();

        protected override async Task OnInitializedAsync()
        {
            var tiersResponse = await TierService.GetTiers();
            if (tiersResponse.Ok)
                tiers = tiersResponse.Result;            

            if (Id.HasValue)
            {
                var response = await Service.GetProductTierById(Id.Value);
                if (response.Ok)
                {
                    productTier = response.Result;
                    if (productTier.Status == EntityStatus.Deleted)
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
                productTier.ProductId = ProductId;

                var submitRs = await Service.UpsertProductTier(new Core.Helpers.AuditWrapDto<ProductTierUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = productTier
                });
                if (submitRs.Ok)
                {

                    DialogService.Close(productTier);
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
