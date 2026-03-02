using ecommerce.Admin.Domain.Dtos.InvoiceTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Core.Utils.ResultSet;
using Radzen.Blazor;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Components.Pages
{
    public partial class InvoiceTypes
    {
        #region Injections
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected TooltipService TooltipService { get; set; }
        [Inject] protected ContextMenuService ContextMenuService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }
        [Inject] public IInvoiceTypeService InvoiceTypeService { get; set; }
        #endregion

        int count;
        protected List<InvoiceTypeListDto> invoiceTypes = null;
        protected RadzenDataGrid<InvoiceTypeListDto>? radzenDataGrid = new();
        private PageSetting pager;
        private readonly DialogOptions dialogOptions = new() { Width = "600px" };

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<Modals.UpsertInvoiceType>("Fatura Tipi Ekle/Düzenle", null, dialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(InvoiceTypeListDto args)
        {
            await DialogService.OpenAsync<Modals.UpsertInvoiceType>("Fatura Tipi Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, dialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, InvoiceTypeListDto data)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen fatura tipini silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await InvoiceTypeService.DeleteInvoiceType(new Core.Helpers.AuditWrapDto<InvoiceTypeDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new InvoiceTypeDeleteDto() { Id = data.Id }
                    });

                    if (deleteResult != null)
                    {
                        if (deleteResult.Ok)
                        {
                            NotificationService.Notify(new NotificationMessage
                            {
                                Severity = NotificationSeverity.Success,
                                Summary = "Silindi",
                                Detail = "Fatura tipi silindi."
                            });
                            await radzenDataGrid.Reload();
                        }
                        else
                            await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions() { OkButtonText = "Tamam" });
                    }
                }
            }
            catch (Exception)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = $"Error",
                    Detail = $"Unable to delete Invoice Type"
                });
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await InvoiceTypeService.GetInvoiceTypes(pager);
            if (response.Ok && response.Result != null)
            {
                invoiceTypes = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());

            StateHasChanged();
        }

        void RowRender(RowRenderEventArgs<InvoiceTypeListDto> args)
        {
            if (args.Data.Status == EntityStatus.Passive)
                args.Attributes.Add("style", $"background-color: #FFEFEF;");
            else if (args.Data.Status == EntityStatus.Deleted)
                args.Attributes.Add("style", $"background-color: #FFE1E1;");
        }
    }
}

