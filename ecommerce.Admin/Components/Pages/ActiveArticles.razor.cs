using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.ActiveArticleDto;
using ecommerce.Admin.Domain.Interfaces;
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
    public partial class ActiveArticles
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
        public IActiveArticlesService Service { get; set; }
        #endregion



        int count;
        protected List<ActiveArticleListDto> activeArticles = null;
        protected RadzenDataGrid<ActiveArticleListDto>? radzenDataGrid = new();
        private PageSetting pager;


        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertActiveArticle>("Etken Madde Ekle/Düzenle", null);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(ActiveArticleListDto args)
        {
            await DialogService.OpenAsync<UpsertActiveArticle>("Etken Madde Düzenle", new Dictionary<string, object> { { "Id", args.Id } });
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, ActiveArticleListDto data)
        {

            try
            {
                if (await DialogService.Confirm("Seçilen etken maddeyi silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await Service.DeleteActiveArticle(new Core.Helpers.AuditWrapDto<ActiveArticleDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new ActiveArticleDeleteDto() { Id = data.Id }
                    });

                    if (deleteResult != null)
                    {
                        if (deleteResult != null)
                        {
                            if (deleteResult.Ok)
                                await radzenDataGrid.Reload();
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
                    Detail = $"Unable to delete Category"
                });
            }
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            //            if (args?.Value == "csv")
            //            {
            //                await ECZAPROService.ExportCategoriesToCSV(new Query
            //{ 
            //    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}", 
            //    OrderBy = $"{grid0.Query.OrderBy}", 
            //    Expand = "", 
            //    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible()).Select(c => c.Property))
            //}, "Categories");
            //            }

            //            if (args == null || args.Value == "xlsx")
            //            {
            //                await ECZAPROService.ExportCategoriesToExcel(new Query
            //{ 
            //    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}", 
            //    OrderBy = $"{grid0.Query.OrderBy}", 
            //    Expand = "", 
            //    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible()).Select(c => c.Property))
            //}, "Categories");
            //            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await Service.GetActiveArticles(pager);
            if (response.Ok && response.Result != null)
            {
                activeArticles = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());

            StateHasChanged();
        }

        void RowRender(RowRenderEventArgs<ActiveArticleListDto> args)
        {
            if (args.Data.Status == EntityStatus.Passive)
                args.Attributes.Add("style", $"background-color: #FFEFEF;");
            else if (args.Data.Status == EntityStatus.Deleted)
                args.Attributes.Add("style", $"background-color: #FFE1E1;");
        }
    }
}
