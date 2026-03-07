using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.SaleOptionsDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages;

public partial class SaleOptions
{
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;
    [Inject] public ISaleOptionsService Service { get; set; } = default!;

    int count;
    protected List<SaleOptionsListDto>? saleOptions;
    protected RadzenDataGrid<SaleOptionsListDto>? grid;
    private PageSetting pager = default!;

    protected async Task LoadData(LoadDataArgs args)
    {
        try
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await Service.GetSaleOptions(pager);
            if (response.Ok && response.Result != null)
            {
                saleOptions = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
        }
    }

    protected async Task AddButtonClick(MouseEventArgs args)
    {
        var result = await DialogService.OpenAsync<UpsertSaleOptions>("Satış Seçeneği Ekle", null, new DialogOptions { Width = "500px" });
        if (result != null && grid != null)
        {
            await grid.Reload();
        }
    }

    protected async Task EditRow(SaleOptionsListDto item)
    {
        var result = await DialogService.OpenAsync<UpsertSaleOptions>("Satış Seçeneği Düzenle",
            new Dictionary<string, object> { { "Id", item.Id } },
            new DialogOptions { Width = "500px" });

        if (result != null && grid != null)
        {
            await grid.Reload();
        }
    }

    protected async Task DeleteRow(SaleOptionsListDto item)
    {
        try
        {
            if ((bool)await DialogService.Confirm("Seçilen satış seçeneğini silmek istediğinize emin misiniz?", "Kayıt Sil",
                    new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" }))
            {
                var response = await Service.DeleteSaleOptions(new AuditWrapDto<SaleOptionsDeleteDto>
                {
                    UserId = Security.User.Id,
                    Dto = new SaleOptionsDeleteDto { Id = item.Id }
                });

                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Satış seçeneği silindi.");
                    if (grid != null)
                        await grid.Reload();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
        }
    }
}
