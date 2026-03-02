using ecommerce.Admin.Domain.Dtos.SupportLineDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Resources;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class SupportLine
    {
        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private NotificationService NotificationService { get; set; }

        [Inject]
        private IStringLocalizer<Culture_TR> RadzenLocalizer { get; set; }

        [Inject]
        private ISupportLineService Service { get; set; }

        private Paging<List<SupportLineListDto>> SupportLines { get; set; } = new();

        private RadzenDataGrid<SupportLineListDto> DataGrid { get; set; }

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

        private async Task EditRow(SupportLineListDto args)
        {
            await DialogService.OpenAsync<UpsertSupportLine>(
                "Talep Görüntüle",
                new Dictionary<string, object>
                {
                    { "Id", args.Id }
                },
                new DialogOptions
                {
                    Width = "900px",
                    CssClass = "mw-100"
                }
            );

            await DataGrid.Reload();
        }

        private async Task DeleteRow(SupportLineListDto productAttribute)
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
                var deleteResult = await Service.DeleteSupportLine(new SupportLineDeleteDto { Id = productAttribute.Id });

                if (deleteResult.Ok)
                {
                    await DataGrid.Reload();
                }
                else
                {
                    await DialogService.Alert(deleteResult.Metadata?.Message, "Uyarı", new AlertOptions { OkButtonText = "Tamam" });
                }
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await Service.GetSupportLines(pager);

            if (response.Ok)
            {
                SupportLines = response.Result;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }
    }
}