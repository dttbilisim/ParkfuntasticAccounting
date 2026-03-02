using ecommerce.Admin.Domain.Dtos.TierDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Core.Utils.ResultSet;
using Radzen.Blazor;
using ecommerce.Core.Utils;
using ecommerce.Admin.Components.Pages.Modals;

namespace ecommerce.Admin.Components.Pages
{
    public partial class Tiers
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
        protected AuthenticationService Security { get; set; }

        [Inject]
        public ITierService TierService { get; set; }
        #endregion

        int count;
        protected List<TierListDto> tiers = null;
        protected RadzenDataGrid<TierListDto>? radzenDataGrid = new();
        private PageSetting pager;


        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertTier>("Ürün Grup / Ekle", null);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(TierListDto args)
        {
            await DialogService.OpenAsync<UpsertTier>("Ürün Grup / Düzenle", new Dictionary<string, object> { { "Id", args.Id } });
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, TierListDto data)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen ürün grubunu silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await TierService.DeleteTier(new Core.Helpers.AuditWrapDto<TierDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new TierDeleteDto() { Id = data.Id }
                    });

                    if (deleteResult != null)
                    {
                        if (deleteResult.Ok)
                            await radzenDataGrid.Reload();
                        else
                            await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions() { OkButtonText = "Tamam" });
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = $"Error",
                    Detail = $"Unable to delete ScaleUnit"
                });
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await TierService.GetTiers(pager);
            if (response.Ok && response.Result != null)
            {
                tiers = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());

            StateHasChanged();
        }

        void RowRender(RowRenderEventArgs<TierListDto> args)
        {
            if (args.Data.Status == EntityStatus.Passive)
                args.Attributes.Add("style", $"background-color: #FFEFEF;");
            else if (args.Data.Status == EntityStatus.Deleted)
                args.Attributes.Add("style", $"background-color: #FFE1E1;");
        }
    }
}
