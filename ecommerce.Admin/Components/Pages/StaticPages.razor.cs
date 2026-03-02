using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ecommerce.Core.Utils.ResultSet;
using Radzen;
using Radzen.Blazor;
using ecommerce.Admin.Components.Pages.Modals;
using Microsoft.AspNetCore.Components.Web;
using ecommerce.Admin.Domain.Dtos.StaticPageDto;

namespace ecommerce.Admin.Components.Pages
{
    public partial class StaticPages
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
        public IStaticPageService StaticPageService { get; set; }

        #endregion

        int count;
        protected List<StaticPageListDto> staticPageList = null;
        protected RadzenDataGrid<StaticPageListDto>? radzenDataGrid = new();
        private PageSetting pager;
        private DialogOptions dialogOptions = new() { Width = "1200px" };

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertStaticPage>("Statik Sayfa Ekle/Düzenle", null, dialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(StaticPageListDto args)
        {
            await DialogService.OpenAsync<UpsertStaticPage>("Statik Sayfa Düzenle", new Dictionary<string, object> {
                { "Id", args.Id }
            }, dialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, StaticPageListDto brand)
        {
            if (await DialogService.Confirm("Seçilen kayıdı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
            {
                OkButtonText = "Evet",
                CancelButtonText = "Hayır"
            }) == true)
            {
                var deleteResult = await StaticPageService.DeleteAboutUs(new Core.Helpers.AuditWrapDto<StaticPageDeleteDto>()
                {
                    UserId = Security.User.Id,
                    Dto = new StaticPageDeleteDto() { Id = brand.Id }
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

        private async Task LoadData(LoadDataArgs args)
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await StaticPageService.GetAboutUs(pager);
            if (response.Ok && response.Result != null)
            {
                staticPageList = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }
    }
}
