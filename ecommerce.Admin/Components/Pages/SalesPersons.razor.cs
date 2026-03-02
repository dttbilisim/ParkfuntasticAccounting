using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
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

namespace ecommerce.Admin.Components.Pages
{
    public partial class SalesPersons
    {
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected TooltipService TooltipService { get; set; }
        [Inject] protected ContextMenuService ContextMenuService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }
        [Inject] public ISalesPersonService SalesPersonService { get; set; }

        int count;
        protected List<SalesPersonListDto> salesPersons = null;
        protected RadzenDataGrid<SalesPersonListDto>? radzenDataGrid = new();
        private PageSetting pager;
        private readonly DialogOptions dialogOptions = new() { Width = "700px" };

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<Modals.UpsertSalesPerson>("Plasiyer Ekle/Düzenle", null, dialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(SalesPersonListDto args)
        {
            await DialogService.OpenAsync<Modals.UpsertSalesPerson>("Plasiyer Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, dialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, SalesPersonListDto data)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen plasiyeri silmek istediğinize emin misiniz?", "Kayıt Sil",
                    new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await SalesPersonService.DeleteSalesPerson(new Core.Helpers.AuditWrapDto<SalesPersonDeleteDto>
                    {
                        UserId = Security.User.Id,
                        Dto = new SalesPersonDeleteDto { Id = data.Id }
                    });

                    if (deleteResult != null)
                    {
                        if (deleteResult.Ok)
                            await radzenDataGrid.Reload();
                        else
                            await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions { OkButtonText = "Tamam" });
                    }
                }
            }
            catch
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Unable to delete SalesPerson"
                });
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await SalesPersonService.GetSalesPersons(pager);
            if (response.Ok && response.Result != null)
            {
                salesPersons = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());

            StateHasChanged();
        }

        void RowRender(RowRenderEventArgs<SalesPersonListDto> args)
        {
            if (args.Data.Status == EntityStatus.Passive)
                args.Attributes.Add("style", "background-color: #FFEFEF;");
            else if (args.Data.Status == EntityStatus.Deleted)
                args.Attributes.Add("style", "background-color: #FFE1E1;");
        }
    }
}