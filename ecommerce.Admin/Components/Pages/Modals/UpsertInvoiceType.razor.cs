using AutoMapper;
using ecommerce.Admin.Domain.Dtos.InvoiceTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Microsoft.AspNetCore.Components.Web;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertInvoiceType
    {
        #region Injection
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected TooltipService TooltipService { get; set; }
        [Inject] protected ContextMenuService ContextMenuService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] public IInvoiceTypeService InvoiceTypeService { get; set; }
        [Inject] public IMapper Mapper { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }
        #endregion

        [Parameter] public int? Id { get; set; }

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected InvoiceTypeUpsertDto invoiceType = new();
        public bool Status { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var response = await InvoiceTypeService.GetInvoiceTypeById(Id.Value);
                if (response.Ok)
                {
                    invoiceType = response.Result;
                    Status = invoiceType.Status == (int)EntityStatus.Passive || invoiceType.Status == (int)EntityStatus.Deleted ? false : true;

                    if (invoiceType.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                invoiceType.Id = Id;
                invoiceType.StatusBool = Status;

                var submitRs = await InvoiceTypeService.UpsertInvoiceType(new Core.Helpers.AuditWrapDto<InvoiceTypeUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = invoiceType
                });
                if (submitRs.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Fatura tipi kaydedildi."
                    });
                    DialogService.Close(invoiceType);
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

