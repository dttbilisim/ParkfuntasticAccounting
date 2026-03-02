using ecommerce.Admin.Domain.Dtos.BannerItemDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class BannerItems
    {
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
        public IBannerItemService Service { get; set; }


        [Inject]
        protected AuthenticationService Security { get; set; }

        int count;
        protected List<BannerItemListDto> banneritems = null;
        protected RadzenDataGrid<BannerItemListDto>? grid0 = new();
        private PageSetting pager;
        private new DialogOptions DialogOptions = new() { Width = "1200px" };

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertBannerItem>("Banner Öğe Ekle/Düzenle", null, DialogOptions);
            await grid0.Reload();
        }

        protected async Task EditRow(BannerItemListDto args)
        {
            await DialogService.OpenAsync<UpsertBannerItem>("Banner Öğe Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
            await grid0.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, BannerItemListDto BannerItem)
        {

            try
            {
                if (await DialogService.Confirm("Seçilen Banner Öğeyi silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await Service.DeleteBannerItem(new Core.Helpers.AuditWrapDto<BannerItemDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new BannerItemDeleteDto() { Id = BannerItem.Id }
                    });

                    if (deleteResult != null)
                    {
                        if (deleteResult != null)
                        {
                            if (deleteResult.Ok)
                                await grid0.Reload();
                            else
                                await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions() { OkButtonText = "Tamam" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = $"Error",
                    Detail = $"Unable to delete BannerItem"
                });
            }
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            //            if (args?.Value == "csv")
            //            {
            //                await ECZAPROService.ExportbanneritemsToCSV(new Query
            //{ 
            //    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}", 
            //    OrderBy = $"{grid0.Query.OrderBy}", 
            //    Expand = "", 
            //    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible()).Select(c => c.Property))
            //}, "banneritems");
            //            }

            //            if (args == null || args.Value == "xlsx")
            //            {
            //                await ECZAPROService.ExportbanneritemsToExcel(new Query
            //{ 
            //    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}", 
            //    OrderBy = $"{grid0.Query.OrderBy}", 
            //    Expand = "", 
            //    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible()).Select(c => c.Property))
            //}, "banneritems");
            //            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            args.Filter = args.Filter.Replace("np", "");
            pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);

            var response = await Service.GetBannerItems(pager);
            if (response.Ok && response.Result.Data != null)
            {
                 var  banneritems1 = response.Result.Data.ToList();
                banneritems = banneritems1;

                foreach (var mdl in banneritems)
                {
                    mdl.Banner.Name = mdl.Banner.Name + " > " + ((BannerType)mdl.Banner.BannerType).GetDisplayDescription();
                }
                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }

        void RowRender(RowRenderEventArgs<BannerItemListDto> args)
        {
            if (args.Data.Status == 0)
                args.Attributes.Add("style", $"background-color: {(args.Data.Status == 0 ? "#FFEFEF" : "White")};");
        }
    }
}