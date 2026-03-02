using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.CargoDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class Cargoes
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
        public ICargoService Service { get; set; }
        #endregion

        int count;
        protected List<CargoListDto> cargoes = null;
        protected RadzenDataGrid<CargoListDto>? radzenDataGrid = new();
        protected DialogOptions DialogOptions = new() { Width="1200px" };
        private PageSetting pager;

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertCargo>("Kargo / Ekle", null, DialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(CargoListDto args)
        {
            await DialogService.OpenAsync<UpsertCargo>("Kargo / Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, CargoListDto data)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen kargoyu silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await Service.DeleteCargo(new Core.Helpers.AuditWrapDto<CargoDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new CargoDeleteDto() { Id = data.Id }
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

            var response = await Service.GetCargoes(pager);
            if (response.Ok && response.Result != null)
            {
                cargoes = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());

            StateHasChanged();
        }
    }
}
