using ecommerce.Admin.Domain.Dtos.BannerDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages{
    public partial class Banners{
        [Inject] protected IJSRuntime JSRuntime{get;set;}
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public IBannerService Service{get;set;}
        [Inject] protected AuthenticationService Security{get;set;}
        int count;
        protected List<BannerListDto> categories = null;
        protected RadzenDataGrid<BannerListDto> ? grid0 = new();
        private PageSetting pager;
        private new DialogOptions DialogOptions = new(){Width = "1200px"};
        protected async Task AddButtonClick(MouseEventArgs args){
            await DialogService.OpenAsync<UpsertBanner>("Banner Ekle/Düzenle", null, DialogOptions);
            await grid0.Reload();
        }
        protected async Task EditRow(BannerListDto args){
            await DialogService.OpenAsync<UpsertBanner>("Banner Düzenle", new Dictionary<string, object>{{"Id", args.Id}}, DialogOptions);
            await grid0.Reload();
        }
        protected async Task GridDeleteButtonClick(MouseEventArgs args, BannerListDto Banner){
            try{
                if(await DialogService.Confirm("Seçilen Banner ı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                    var deleteResult = await Service.DeleteBanner(new Core.Helpers.AuditWrapDto<BannerDeleteDto>(){UserId = Security.User.Id, Dto = new BannerDeleteDto(){Id = Banner.Id}});
                    if(deleteResult != null){
                        if(deleteResult != null){
                            if(deleteResult.Ok)
                                await grid0.Reload();
                            else
                                await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions(){OkButtonText = "Tamam"});
                        }
                    }
                }
            } catch(Exception ex){
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete Banner"});
            }
        }
        protected async Task ExportClick(RadzenSplitButtonItem args){
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
        private async Task LoadData(LoadDataArgs args){
            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            args.Filter = args.Filter.Replace("np", "");
            pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);
            var response = await Service.GetBanners(pager);
            if(response.Ok && response.Result != null){
                categories = response.Result.Data.ToList();
                count = response.Result.DataCount;
            } else{
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }
        void RowRender(RowRenderEventArgs<BannerListDto> args){
            if(args.Data.Status == 0) args.Attributes.Add("style", $"background-color: {(args.Data.Status == 0 ? "#FFEFEF" : "White")};");
        }
    }
}
