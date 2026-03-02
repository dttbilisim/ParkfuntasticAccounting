using ecommerce.Admin.Domain.Dtos.WarehouseShelfDto;
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
    public partial class WarehouseShelves
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
        public IWarehouseShelfService Service { get; set; }

        #endregion

        [Parameter]
        public int WarehouseId { get; set; }

        int count;
        protected List<WarehouseShelfListDto> shelves = null;
        protected RadzenDataGrid<WarehouseShelfListDto>? radzenDataGrid = new();
        private PageSetting pager;

        protected void GoBack()
        {
            NavigationManager.NavigateTo("/warehouses");
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertWarehouseShelf>("Raf Ekle/Düzenle", new Dictionary<string, object> {
                { "WarehouseId", WarehouseId }
            });
            await radzenDataGrid.Reload();
        }

        protected async Task BatchCreateClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertBatchWarehouseShelf>("Toplu Raf Oluştur", new Dictionary<string, object> {
                { "WarehouseId", WarehouseId }
            });
            await radzenDataGrid.Reload();
        }

        protected IList<WarehouseShelfListDto> selectedShelves;
        
        protected async Task BatchDeleteClick(MouseEventArgs args)
        {
            if (selectedShelves == null || !selectedShelves.Any())
            {
                await DialogService.Alert("Lütfen silinecek rafları seçiniz.", "Uyarı");
                return;
            }

            if (await DialogService.Confirm($"{selectedShelves.Count} adet rafı silmek istediğinize emin misiniz?", "Toplu Silme", new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
            {
                 var model = new Core.Helpers.AuditWrapDto<WarehouseShelfBatchDeleteDto>
                 {
                     UserId = Security.User.Id,
                     Dto = new WarehouseShelfBatchDeleteDto
                     {
                         Ids = selectedShelves.Select(x => x.Id).ToList()
                     }
                 };

                 var response = await Service.BatchDeleteShelves(model);
                 if (response.Ok)
                 {
                     NotificationService.Notify(NotificationSeverity.Success, "Başarılı", response.GetMetadataMessages());
                     selectedShelves = null;
                     await radzenDataGrid.Reload();
                 }
                 else
                 {
                     NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                 }
            }
        }

        protected async Task EditRow(WarehouseShelfListDto args)
        {
            await DialogService.OpenAsync<UpsertWarehouseShelf>("Raf Düzenle", new Dictionary<string, object> {
                { "Id", args.Id },
                { "WarehouseId", WarehouseId }
            });
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, WarehouseShelfListDto item)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen rafı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await Service.DeleteShelf(new Core.Helpers.AuditWrapDto<WarehouseShelfDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new WarehouseShelfDeleteDto() { Id = item.Id }
                    });

                    if (deleteResult != null)
                    {
                        if (deleteResult.Ok)
                        {
                            NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Raf silindi");
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
            // Note: Currently GetShelves returns all shelves, better to filter by WarehouseId in Backend or Client
            // For now, I'll filter client side if the API doesn't support direct filtering via PageSetting easily (unless i implemented a custom search)
            // Ideally, I should add a GetShelvesByWarehouse with paging, but I used the generic GetShelves method.
            // Let's rely on the generic filtering for now but I will inject the WarehouseId into the filter if empty.
            
            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            
            // Add WarehouseId filter
             string filter = args.Filter.Replace("np", "");
            if (string.IsNullOrEmpty(filter))
            {
                filter = $"WarehouseId={WarehouseId}";
            }
            else
            {
                filter = $"({filter}) and WarehouseId={WarehouseId}";
            }

            pager = new PageSetting(filter, orderfilter, args.Skip, args.Top);

            var response = await Service.GetShelves(pager);
            if (response.Ok && response.Result != null)
            {
                shelves = response.Result.Data.ToList();
                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }

         void RowRender(RowRenderEventArgs<WarehouseShelfListDto> args)
        {
            if (args.Data.Status == 0)
                args.Attributes.Add("style", $"background-color: {(args.Data.Status == 0 ? "#FFEFEF" : "White")};");
        }
    }
}
