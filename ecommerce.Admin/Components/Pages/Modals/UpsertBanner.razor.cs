using AutoMapper;
using ecommerce.Admin.Domain.Dtos.BannerDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertBanner
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
        public IBannerService BannerService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }
        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        #endregion


        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected BannerUpsertDto Banner = new();
        public BannerListDto _BannerCalendar = new();
        public bool Status { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var response = await BannerService.GetBannerById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    Banner = response.Result;
                    Status = Banner.Status == (int)EntityStatus.Passive || Banner.Status == (int)EntityStatus.Deleted ? false : true;

                    if (Banner.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            else
            {
                BannerTypeChange(BannerType.Banner);
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                Banner.Id = Id;
                Banner.StatusBool = Status;

                var susscesOrder = await BannerService.GetBannerLastCount(Banner.BannerType);
                if (Id==null && Banner.Order < susscesOrder)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Bu sıra da zaten kayıt var");
                }
                else
                {
                    var submitRs = await BannerService.UpsertBanner(new Core.Helpers.AuditWrapDto<BannerUpsertDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = Banner
                    });
                    if (submitRs.Ok)
                    {

                        DialogService.Close(Banner);
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                    }
                }
            }
            catch (Exception ex)
            {
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }
        protected async void BannerTypeChange(object args)
        {
            var count = await BannerService.GetBannerLastCount((BannerType)args);
            Banner.Order = count;
            DialogService.Refresh();
        }
        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }

    }
}
