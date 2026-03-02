using ecommerce.Admin.Domain.Dtos.CourierDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class CourierVehiclesModal
{
    [Parameter] public int CourierId { get; set; }
    [Parameter] public string CourierName { get; set; } = "";
    [Inject] protected ICourierService CourierService { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;

    protected string Title => $"Araçlar — {CourierName}";
    protected List<CourierVehicleListDto> Vehicles { get; set; } = new();
    protected bool Loading { get; set; } = true;
    protected bool Saving { get; set; }
    protected int? NewVehicleType { get; set; }
    protected string NewLicensePlate { get; set; } = "";
    protected string NewDriverName { get; set; } = "";
    protected string NewDriverPhone { get; set; } = "";

    private static readonly List<(int Value, string Label)> VehicleTypes = new()
    {
        (0, "Motosiklet"),
        (1, "Bisiklet"),
        (2, "Otomobil"),
        (3, "Kamyonet"),
    };

    protected override async Task OnInitializedAsync()
    {
        var result = await CourierService.GetVehicles(CourierId);
        if (result.Ok && result.Result != null)
            Vehicles = result.Result.ToList();
        NewVehicleType = 0;
        Loading = false;
    }

    private async Task AddVehicle()
    {
        var plate = (NewLicensePlate ?? "").Trim();
        if (string.IsNullOrEmpty(plate))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Plaka girin.");
            return;
        }
        var type = NewVehicleType ?? 0;
        var dto = new CourierVehicleUpsertDto
        {
            VehicleType = (CourierVehicleType)type,
            LicensePlate = plate,
            DriverName = string.IsNullOrWhiteSpace(NewDriverName) ? null : NewDriverName.Trim(),
            DriverPhone = string.IsNullOrWhiteSpace(NewDriverPhone) ? null : NewDriverPhone.Trim()
        };
        await SaveVehicle(dto);
        NewLicensePlate = "";
        NewDriverName = "";
        NewDriverPhone = "";
        NewVehicleType = 0;
    }

    private async Task SaveVehicle(CourierVehicleUpsertDto dto)
    {
        Saving = true;
        try
        {
            var result = await CourierService.SaveVehicle(CourierId, dto);
            if (result.Ok && result.Result != null)
            {
                Vehicles.Add(result.Result);
                NotificationService.Notify(NotificationSeverity.Success, "Araç eklendi.");
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

    private async Task DeleteVehicle(CourierVehicleListDto v)
    {
        var result = await CourierService.DeleteVehicle(CourierId, v.Id);
        if (result.Ok)
        {
            Vehicles.Remove(v);
            NotificationService.Notify(NotificationSeverity.Success, "Araç silindi.");
        }
        else
            NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
        await InvokeAsync(StateHasChanged);
    }
}
