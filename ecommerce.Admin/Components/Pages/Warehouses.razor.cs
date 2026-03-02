using ecommerce.Admin.Domain.Dtos.WarehouseDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;



namespace ecommerce.Admin.Components.Pages
{
    public partial class Warehouses
    {
        #region Injection

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Inject]
        public IWarehouseService Service { get; set; }



        #endregion

        int count;
        protected List<WarehouseListDto> warehouses = null;
        protected RadzenDataGrid<WarehouseListDto>? radzenDataGrid = new();
        private PageSetting pager;


        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertWarehouse>(null, null, new DialogOptions 
            { 
               Width = "800px", 
               Height = "auto", 
               Style = "max-height: 90vh;", 
               ShowTitle = false, 
               ShowClose = false,
               Resizable = true, 
               Draggable = true 
            });
            await radzenDataGrid.Reload();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
               // await radzenDataGrid.SetTurkishTexts(RadzenLocalizer);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        protected async Task EditRow(WarehouseListDto args)
        {
            await DialogService.OpenAsync<UpsertWarehouse>(null, new Dictionary<string, object> {
                { "Id", args.Id }
            }, new DialogOptions 
            { 
               Width = "800px", 
               Height = "auto", 
               Style = "max-height: 90vh;", 
               ShowTitle = false, 
               ShowClose = false,
               Resizable = true, 
               Draggable = true 
            });
            await radzenDataGrid.Reload();
        }

        protected void NavigateToShelves(MouseEventArgs args, WarehouseListDto item)
        {
            NavigationManager.NavigateTo($"/warehouses/{item.Id}/shelves");
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, WarehouseListDto item)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen depoyu silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await Service.DeleteWarehouse(new Core.Helpers.AuditWrapDto<WarehouseDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new WarehouseDeleteDto() { Id = item.Id }
                    });

                    if (deleteResult != null)
                    {
                        if (deleteResult.Ok)
                        {
                            NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Depo silindi.");
                            await radzenDataGrid.Reload();
                        }
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
                    Summary = "Hata",
                    Detail = "Silme işlemi sırasında hata oluştu"
                });
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            args.Filter = args.Filter.Replace("np", "");
            pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);

            var response = await Service.GetWarehouses(pager);
            if (response.Ok && response.Result != null)
            {
                warehouses = response.Result.Data.ToList();
                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }

        void RowRender(RowRenderEventArgs<WarehouseListDto> args)
        {
            if (args.Data.Status == 0)
                args.Attributes.Add("style", $"background-color: {(args.Data.Status == 0 ? "#FFEFEF" : "White")};");
        }
    }
}
