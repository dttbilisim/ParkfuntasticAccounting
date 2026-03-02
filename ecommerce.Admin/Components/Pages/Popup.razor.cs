using ecommerce.Admin.Domain.Dtos.PopupDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Resources;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class Popup
    {
        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private NotificationService NotificationService { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        [Inject]
        private IStringLocalizer<Culture_TR> RadzenLocalizer { get; set; }

        [Inject]
        private IPopupService Service { get; set; }

        private Paging<List<PopupListDto>> Popups { get; set; } = new();

        private RadzenDataGrid<PopupListDto> DataGrid { get; set; }

        private void OnRadzenGridRender<TItem>(DataGridRenderEventArgs<TItem> args)
        {
            if (!args.FirstRender)
            {
                return;
            }

            _ = SetRadzenTexts(args.Grid);
        }

        private async Task SetRadzenTexts(RadzenComponent radzenComponent)
        {
            var parameters = ParameterView.FromDictionary(
                RadzenLocalizer.GetAllStrings().ToDictionary(l => l.Name, l => (object?) l.Value)
            );

            await radzenComponent.SetParametersAsync(parameters);

            await InvokeAsync(StateHasChanged);
        }

        private async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertPopup>(
                "Popup Ekle",
                options: new DialogOptions
                {
                    Width = "1200px",
                    CssClass = "mw-100"
                }
            );

            await DataGrid.Reload();
        }

        private async Task EditRow(PopupListDto args)
        {
            await DialogService.OpenAsync<UpsertPopup>(
                "Popup Düzenle",
                new Dictionary<string, object>
                {
                    { "Id", args.Id }
                },
                options: new DialogOptions
                {
                    Width = "1200px",
                    CssClass = "mw-100"
                }
            );
            await DataGrid.Reload();
        }

        private async Task GridDeleteButtonClick(PopupListDto Popup)
        {
            if (await DialogService.Confirm(
                    "Seçilen kaydı silmek istediğinize emin misiniz?",
                    "Kayıt Sil",
                    new ConfirmOptions()
                    {
                        OkButtonText = "Evet",
                        CancelButtonText = "Hayır"
                    }
                ) == true)
            {
                var deleteResult = await Service.DeletePopup(new PopupDeleteDto() { Id = Popup.Id });

                if (deleteResult.Ok)
                {
                    await DataGrid.Reload();
                }
                else
                {
                    await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions() { OkButtonText = "Tamam" });
                }
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            args.Filter = args.Filter.Replace("np", "");
            var pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);

            var response = await Service.GetPopups(pager);
            if (response.Ok)
            {
                Popups = response.Result;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }
    }
}