using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class CourierServiceAreasModal
{
    [Parameter] public int CourierId { get; set; }
    [Parameter] public string CourierName { get; set; } = "";
    [Inject] protected ICourierService CourierService { get; set; } = null!;
    [Inject] protected ICityService CityService { get; set; } = null!;
    [Inject] protected ITownService TownService { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;

    protected string Title => $"Hizmet bölgeleri — {CourierName}";
    protected List<CourierServiceAreaListDto> Areas { get; set; } = new();
    protected List<CourierVehicleListDto> Vehicles { get; set; } = new();
    protected List<CityListDto> Cities { get; set; } = new();
    protected List<TownListDto> Towns { get; set; } = new();
    protected int? NewVehicleId { get; set; }
    protected int? NewCityId { get; set; }
    protected int? NewTownId { get; set; }
    protected bool Loading { get; set; } = true;
    protected bool Saving { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var cityResult = await CityService.GetCities();
        if (cityResult.Ok && cityResult.Result != null)
            Cities = cityResult.Result;

        var vehiclesResult = await CourierService.GetVehicles(CourierId);
        if (vehiclesResult.Ok && vehiclesResult.Result != null)
            Vehicles = vehiclesResult.Result.ToList();

        var areasResult = await CourierService.GetServiceAreas(CourierId);
        if (areasResult.Ok && areasResult.Result != null)
            Areas = areasResult.Result.ToList();

        Loading = false;
    }

    private void OnVehicleValueChanged(int? value)
    {
        NewVehicleId = value;
        StateHasChanged();
    }

    private async Task OnCityValueChanged(int? value)
    {
        NewCityId = value;
        NewTownId = null;
        Towns = new List<TownListDto>();
        if (value.HasValue && value.Value > 0)
        {
            var townResult = await TownService.GetTownsByCityId(value.Value);
            if (townResult.Ok && townResult.Result != null)
                Towns = townResult.Result;
        }
        await InvokeAsync(StateHasChanged);
    }

    private void OnTownValueChanged(int? value)
    {
        NewTownId = value;
        StateHasChanged();
    }

    private async Task OnAddArea()
    {
        if (NewCityId == null || NewTownId == null || NewCityId == 0 || NewTownId == 0)
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Lütfen önce il ve ilçe seçin.");
            return;
        }
        var cityName = Cities.FirstOrDefault(c => c.Id == NewCityId)?.Name ?? "";
        var townName = Towns.FirstOrDefault(t => t.Id == NewTownId)?.Name ?? "";
        var vehicleId = NewVehicleId > 0 ? NewVehicleId : (int?)null;
        var vehicleDisplay = vehicleId.HasValue ? Vehicles.FirstOrDefault(v => v.Id == vehicleId)?.VehicleTypeName + " - " + Vehicles.FirstOrDefault(v => v.Id == vehicleId)?.LicensePlate : null;
        if (Areas.Any(a => a.CourierVehicleId == vehicleId && a.CityId == NewCityId && a.TownId == NewTownId))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Bu araç için bu il/ilçe zaten listede.");
            return;
        }
        Areas.Add(new CourierServiceAreaListDto
        {
            CourierVehicleId = vehicleId,
            VehicleDisplay = vehicleDisplay,
            CityId = NewCityId.Value,
            CityName = cityName,
            TownId = NewTownId.Value,
            TownName = townName,
            WorkStartTime = null,
            WorkEndTime = null
        });
        NewCityId = null;
        NewTownId = null;
        NewVehicleId = null;
        Towns = new List<TownListDto>();
        await InvokeAsync(StateHasChanged);
    }

    private void RemoveArea(CourierServiceAreaListDto row)
    {
        Areas.Remove(row);
        StateHasChanged();
    }

    private async Task Save()
    {
        var upsertList = Areas.Select(a => new CourierServiceAreaUpsertDto
        {
            CourierVehicleId = a.CourierVehicleId > 0 ? a.CourierVehicleId : null,
            CityId = a.CityId,
            TownId = a.TownId,
            NeighboorId = a.NeighboorId,
            WorkStartTime = a.WorkStartTime,
            WorkEndTime = a.WorkEndTime
        }).ToList();
        Saving = true;
        try
        {
            var result = await CourierService.SaveServiceAreas(CourierId, upsertList);
            if (result.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Hizmet bölgeleri kaydedildi.");
                DialogService.Close(true);
            }
            else
                NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
        }
        finally
        {
            Saving = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
