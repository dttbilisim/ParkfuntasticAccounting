using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductStockDto;
using ecommerce.Admin.Domain.Dtos.WarehouseDto;
using ecommerce.Admin.Domain.Dtos.WarehouseShelfDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertProductStock
    {
        #region Injections

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public IProductStockService Service { get; set; }

        [Inject]
        public IWarehouseService WarehouseService { get; set; }

        [Inject]
        public IWarehouseShelfService ShelfService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }
        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        [Parameter]
        public int ProductId { get; set; }

        #endregion

        protected bool errorVisible;
        protected ProductStockUpsertDto dto = new();
        public bool Status { get; set; } = true;
        
        protected List<WarehouseListDto> warehouses = new();
        protected List<WarehouseShelfListDto> shelves = new();
        protected int selectedWarehouseId;

        protected override async Task OnInitializedAsync()
        {
            // Load Warehouses
            var warehouseResponse = await WarehouseService.GetAllWarehouses();
            if (warehouseResponse.Ok) warehouses = warehouseResponse.Result;

            if (Id.HasValue && Id.Value > 0)
            {
                var response = await Service.GetStockById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    dto = response.Result;
                    Status = dto.Status == (int)EntityStatus.Active;
                    
                    // Load Shelf info to find Warehouse
                    if(dto.WarehouseShelfId > 0)
                    {
                        var shelfResponse = await ShelfService.GetShelfById(dto.WarehouseShelfId);
                        if(shelfResponse.Ok && shelfResponse.Result != null)
                        {
                            selectedWarehouseId = shelfResponse.Result.WarehouseId;
                            await LoadShelves(selectedWarehouseId);
                        }
                    }
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            else
            {
                dto.ProductId = ProductId;
            }
        }
        
        protected async Task OnWarehouseChange(object value)
        {
             if (value is int warehouseId)
             {
                 await LoadShelves(warehouseId);
             }
        }

        private async Task LoadShelves(int warehouseId)
        {
             var shelvesResponse = await ShelfService.GetShelvesByWarehouse(warehouseId);
             if (shelvesResponse.Ok) 
             {
                 shelves = shelvesResponse.Result;
             }
             else
             {
                 shelves = new List<WarehouseShelfListDto>();
             }
        }

        protected async Task FormSubmit()
        {
            try
            {
                dto.Id = Id;
                dto.StatusBool = Status;
                dto.ProductId = ProductId;

                var submitRs = await Service.UpsertStock(new Core.Helpers.AuditWrapDto<ProductStockUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = dto
                });
                if (submitRs.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "İşlem Başarılı");
                    DialogService.Close(dto);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }
    }
}
