using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.CurrencyDto;
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

public partial class Currencies
{
    #region Injections

    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;
    [Inject] public ICurrencyAdminService Service { get; set; } = default!;

    #endregion

    int count;
    protected List<CurrencyListDto>? currencies;
    protected RadzenDataGrid<CurrencyListDto>? radzenDataGrid = new();
    private PageSetting pager = default!;

    protected async Task AddButtonClick(MouseEventArgs args)
    {
        var result = await DialogService.OpenAsync<UpsertCurrency>("Kur Ekle", null, new DialogOptions { Width = "600px" });
        if (result != null && radzenDataGrid != null)
        {
            await radzenDataGrid.Reload();
        }
    }

    protected async Task RefreshCurrenciesClick(MouseEventArgs args)
    {
        try
        {
            var confirm = await DialogService.Confirm(
                "Merkez bankasından güncel kurları çekmek istiyor musunuz? Sabit (IsStatic işaretli) kurlar güncellenmeyecektir.",
                "Güncel Kurları Çek",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

            if (confirm == true)
            {
                var response = await Service.RefreshCurrenciesFromCurrencyData(Security.User.Id);
                if (response.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Güncel kurlar başarıyla çekildi. Sabit işaretli kurlar değiştirilmedi."
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

    protected async Task EditRow(CurrencyListDto args)
    {
        var result = await DialogService.OpenAsync<UpsertCurrency>("Kur Düzenle",
            new Dictionary<string, object> { { "Id", args.Id } },
            new DialogOptions { Width = "600px" });

        if (result != null && radzenDataGrid != null)
        {
            await radzenDataGrid.Reload();
        }
    }

    protected async Task GridDeleteButtonClick(MouseEventArgs args, CurrencyListDto data)
    {
        try
        {
            if (await DialogService.Confirm("Seçilen kuru silmek istediğinize emin misiniz?", "Kayıt Sil",
                    new ConfirmOptions
                    {
                        OkButtonText = "Evet",
                        CancelButtonText = "Hayır"
                    }) == true)
            {
                var deleteResult = await Service.DeleteCurrency(new AuditWrapDto<CurrencyDeleteDto>
                {
                    UserId = Security.User.Id,
                    Dto = new CurrencyDeleteDto { Id = data.Id }
                });

                if (deleteResult != null)
                {
                    if (deleteResult.Ok)
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Başarılı",
                            Detail = "Kur başarıyla silindi."
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
        }
        catch (Exception)
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
        pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

        var response = await Service.GetCurrencies(pager);
        if (response.Ok && response.Result != null)
        {
            currencies = response.Result.Data?.ToList();
            count = response.Result.DataCount;
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }

        StateHasChanged();
    }
}


