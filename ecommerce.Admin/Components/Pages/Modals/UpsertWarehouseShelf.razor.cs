using AutoMapper;
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
    public partial class UpsertWarehouseShelf
    {
        #region Injections

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public IWarehouseShelfService Service { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }
        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        [Parameter]
        public int WarehouseId { get; set; }

        #endregion

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected WarehouseShelfUpsertDto dto = new();
        public bool Status { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue && Id.Value > 0)
            {
                var response = await Service.GetShelfById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    dto = response.Result;
                    Status = dto.Status == (int)EntityStatus.Active;

                    if (dto.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            else
            {
                // New Shelf
                dto.WarehouseId = WarehouseId;
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                dto.Id = Id;
                dto.StatusBool = Status;
                // Ensure WarehouseId is preserved
                if (dto.WarehouseId == 0) dto.WarehouseId = WarehouseId;

                var submitRs = await Service.UpsertShelf(new Core.Helpers.AuditWrapDto<WarehouseShelfUpsertDto>()
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
