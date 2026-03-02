using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class CashRegisters
    {
        [Inject] protected DialogService DialogService { get; set; } = default!;
        [Inject] protected NotificationService NotificationService { get; set; } = default!;
        [Inject] protected AuthenticationService Security { get; set; } = default!;
        [Inject] public ICashRegisterService Service { get; set; } = default!;

        int count;
        protected List<CashRegisterListDto>? cashRegisters;
        protected RadzenDataGrid<CashRegisterListDto>? radzenDataGrid = new();
        private PageSetting pager = default!;

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            var result = await DialogService.OpenAsync<UpsertCashRegister>("Kasa Ekle", null, new DialogOptions { Width = "700px" });
            if (result != null && radzenDataGrid != null)
            {
                await radzenDataGrid.Reload();
            }
        }

        protected async Task EditRow(CashRegisterListDto args)
        {
            var result = await DialogService.OpenAsync<UpsertCashRegister>("Kasa Düzenle",
                new Dictionary<string, object> { { "Id", args.Id } },
                new DialogOptions { Width = "700px" });

            if (result != null && radzenDataGrid != null)
            {
                await radzenDataGrid.Reload();
            }
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, CashRegisterListDto data)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen kasayı silmek istediğinize emin misiniz?", "Kayıt Sil",
                        new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await Service.DeleteCashRegister(new AuditWrapDto<CashRegisterDeleteDto>
                    {
                        UserId = Security.User.Id,
                        Dto = new CashRegisterDeleteDto { Id = data.Id }
                    });

                    if (deleteResult.Ok)
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Başarılı",
                            Detail = "Kasa başarıyla silindi."
                        });

                        if (radzenDataGrid != null)
                            await radzenDataGrid.Reload();
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Error, deleteResult.GetMetadataMessages());
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
            var response = await Service.GetCashRegisters(pager);
            if (response.Ok && response.Result != null)
            {
                cashRegisters = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }
    }
}
