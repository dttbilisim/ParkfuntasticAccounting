using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor;
using Radzen;
using ecommerce.Admin.Services;
using ecommerce.Admin.Domain.Dtos.NotificationTypeDto;
using ecommerce.Admin.Components.Pages.Modals;
using Microsoft.AspNetCore.Components.Web;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Components.Pages
{
    public partial class NotificationTypes
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
        public INotificationTypeService Service { get; set; }
        #endregion

        int count;
        protected List<NotificationTypeListDto> notificationTypes = null;
        protected RadzenDataGrid<NotificationTypeListDto>? radzenDataGrid = new();
        private PageSetting pager;

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertNotificationType>("Bildirim Tipi / Ekle", null);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(NotificationTypeListDto args)
        {
            await DialogService.OpenAsync<UpsertNotificationType>("Bildirim Tipi / Düzenle", new Dictionary<string, object> { { "Id", args.Id } });
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, NotificationTypeListDto data)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen bildirimi silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await Service.DeleteNotificationType(new Core.Helpers.AuditWrapDto<NotificationTypeDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new NotificationTypeDeleteDto() { Id = data.Id }
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
                    Detail = $"Unable to delete ScaleUnit"
                });
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            args.Filter = args.Filter.Replace("np", "");
            pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);

            var response = await Service.GetNotificationTypes(pager);
            if (response.Ok && response.Result != null)
            {
                notificationTypes = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());

            StateHasChanged();
        }

    }
}
