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
    public partial class UpsertBatchWarehouseShelf
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
        public int WarehouseId { get; set; }

        #endregion

        protected bool errorVisible;
        protected WarehouseShelfBatchCreateDto dto = new();
        public bool Status { get; set; } = true;

        protected string PreviewText = "";

        protected override void OnInitialized()
        {
             dto.WarehouseId = WarehouseId;
             dto.StartNumber = 1;
             dto.EndNumber = 10;
             UpdatePreview();
        }

        protected void UpdatePreview()
        {
            if (dto.WarehouseId == 0)
            {
                // Warn or fix? Assuming it's set correctly.
            }

            var p = dto.Prefix ?? "";
            var s = dto.Suffix ?? "";
            var start = dto.StartNumber;
            var end = dto.EndNumber;
            
            if (end < start) end = start;

            if (end - start > 5)
            {
                PreviewText = $"{p}{start}{s}, {p}{start + 1}{s} ... {p}{end}{s}";
            }
            else
            {
                var list = new List<string>();
                for (int i = start; i <= end; i++)
                {
                    list.Add($"{p}{i}{s}");
                }
                PreviewText = string.Join(", ", list);
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                if (WarehouseId == 0)
                {
                     NotificationService.Notify(NotificationSeverity.Error, "Hata", "Depo ID bulunamadı.");
                     return;
                }

                dto.StatusBool = Status;
                dto.WarehouseId = WarehouseId;

                var submitRs = await Service.BatchCreateShelves(new Core.Helpers.AuditWrapDto<WarehouseShelfBatchCreateDto>()
                {
                    UserId = Security.User.Id,
                    Dto = dto
                });
                if (submitRs.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, submitRs.GetMetadataMessages());
                    DialogService.Close(true);
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
