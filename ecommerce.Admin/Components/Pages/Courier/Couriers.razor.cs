using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.Courier;

public partial class Couriers
{
    [Inject] protected ICourierService CourierService { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;

    protected List<CourierListDto>? Items { get; set; }
    protected int Count { get; set; }
    protected RadzenDataGrid<CourierListDto>? Grid;

    private PageSetting _pager = new();
    private bool _initialLoadDone;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_initialLoadDone)
        {
            _initialLoadDone = true;
            await LoadData(new LoadDataArgs { Skip = 0, Top = 25 });
        }
    }

    protected async Task LoadData(LoadDataArgs args)
    {
        try
        {
            _pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top ?? 25);
            var response = await CourierService.GetPaged(_pager, null);

            if (response.Ok && response.Result != null)
            {
                Items = response.Result.Data ?? new List<CourierListDto>();
                Count = response.Result.DataCount;
            }
            else
            {
                Items = new List<CourierListDto>();
                Count = 0;
                if (!response.Ok)
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
            Items = new List<CourierListDto>();
            Count = 0;
        }
        await InvokeAsync(StateHasChanged);
    }

    protected async Task OpenServiceAreas(CourierListDto courier)
    {
        var result = await DialogService.OpenAsync<ecommerce.Admin.Components.Pages.Modals.CourierServiceAreasModal>(
            $"Hizmet bölgeleri — {courier.UserName}",
            new Dictionary<string, object> { { "CourierId", courier.Id }, { "CourierName", courier.UserName } },
            new DialogOptions { Width = "720px", Resizable = true, Draggable = true });
        if (result == true && Grid != null)
            await Grid.Reload();
    }

    protected async Task OpenVehicles(CourierListDto courier)
    {
        await DialogService.OpenAsync<ecommerce.Admin.Components.Pages.Modals.CourierVehiclesModal>(
            $"Araçlar — {courier.UserName}",
            new Dictionary<string, object> { { "CourierId", courier.Id }, { "CourierName", courier.UserName } },
            new DialogOptions { Width = "520px", Resizable = true, Draggable = true });
    }
}
