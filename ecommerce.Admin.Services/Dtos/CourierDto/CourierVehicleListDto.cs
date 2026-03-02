using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CourierDto;

public class CourierVehicleListDto
{
    public int Id { get; set; }
    public int CourierId { get; set; }
    public CourierVehicleType VehicleType { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    /// <summary>Bağlı alt kullanıcı (şoför) ID. Mobilde seçim için kullanılır.</summary>
    public int? DriverUserId { get; set; }
    /// <summary>Dropdown vb. için: "Motosiklet - 34 ABC 123"</summary>
    public string Display => $"{VehicleTypeName} - {LicensePlate}";
}
