using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CourierDto;

public class CourierVehicleUpsertDto
{
    public int? Id { get; set; }
    public CourierVehicleType VehicleType { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    /// <summary>Alt kullanıcı (şoför) ID. Verilirse DriverName/DriverPhone bu kullanıcıdan doldurulur.</summary>
    public int? DriverUserId { get; set; }
}
