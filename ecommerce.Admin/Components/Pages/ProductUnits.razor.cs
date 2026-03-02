using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.ProductUnitDto;
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

public partial class ProductUnits
{
    #region Injections

    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;
    [Inject] public IProductUnitService Service { get; set; } = default!;

    #endregion

    int count;
    protected List<ProductUnitListDto>? productUnits;
    protected RadzenDataGrid<ProductUnitListDto>? radzenDataGrid = new();
    private PageSetting pager = default!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    protected async Task LoadData(LoadDataArgs args)
    {
        try
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await Service.GetProductUnits(pager);
            if (response.Ok && response.Result != null)
            {
                productUnits = response.Result.Data?.ToList();
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
        var result = await DialogService.OpenAsync<UpsertProductUnit>("Ürün Birimi Ekle", null, new DialogOptions { Width = "700px" });
        if (result != null && radzenDataGrid != null)
        {
            await radzenDataGrid.Reload();
        }
    }

    protected async Task EditRow(ProductUnitListDto args)
    {
        var result = await DialogService.OpenAsync<UpsertProductUnit>("Ürün Birimi Düzenle",
            new Dictionary<string, object> { { "Id", args.Id } },
            new DialogOptions { Width = "700px" });

        if (result != null && radzenDataGrid != null)
        {
            await radzenDataGrid.Reload();
        }
    }

    protected async Task GridDeleteButtonClick(MouseEventArgs args, ProductUnitListDto data)
    {
        try
        {
            if ((bool)await DialogService.Confirm("Seçilen ürün birimini silmek istediğinize emin misiniz?", "Kayıt Sil",
                    new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" }))
            {
                var response = await Service.DeleteProductUnit(new AuditWrapDto<ProductUnitDeleteDto>
                {
                    UserId = Security.User.Id,
                    Dto = new ProductUnitDeleteDto { Id = data.Id }
                });

                if (response.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Ürün birimi silindi."
                    });

                    if (radzenDataGrid != null)
                    {
                        await radzenDataGrid.Reload();
                    }
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
