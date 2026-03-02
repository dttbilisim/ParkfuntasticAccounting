using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.UnitDto;
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

public partial class Units
{
    #region Injections

    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;
    [Inject] public IUnitService Service { get; set; } = default!;

    #endregion

    int count;
    protected List<UnitListDto>? units;
    protected RadzenDataGrid<UnitListDto>? radzenDataGrid = new();
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

            var response = await Service.GetUnits(pager);
            if (response.Ok && response.Result != null)
            {
                units = response.Result.Data?.ToList();
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
        var result = await DialogService.OpenAsync<UpsertUnit>("Birim Ekle", null, new DialogOptions { Width = "600px" });
        if (result != null && radzenDataGrid != null)
        {
            await radzenDataGrid.Reload();
        }
    }

    protected async Task EditRow(UnitListDto args)
    {
        var result = await DialogService.OpenAsync<UpsertUnit>("Birim Düzenle",
            new Dictionary<string, object> { { "Id", args.Id } },
            new DialogOptions { Width = "600px" });

        if (result != null && radzenDataGrid != null)
        {
            await radzenDataGrid.Reload();
        }
    }

    protected async Task GridDeleteButtonClick(MouseEventArgs args, UnitListDto data)
    {
        try
        {
            if ((bool)await DialogService.Confirm("Seçilen birimi silmek istediğinize emin misiniz?", "Kayıt Sil",
                    new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" }))
            {
                var response = await Service.DeleteUnit(new AuditWrapDto<UnitDeleteDto>
                {
                    UserId = Security.User.Id,
                    Dto = new UnitDeleteDto { Id = data.Id }
                });

                if (response.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Birim silindi."
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
