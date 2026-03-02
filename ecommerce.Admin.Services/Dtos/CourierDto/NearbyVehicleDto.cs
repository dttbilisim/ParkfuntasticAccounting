namespace ecommerce.Admin.Domain.Dtos.CourierDto;

/// <summary>Haritada gösterilen yakındaki araç (kurye konumu = aracın anlık konumu).</summary>
public class NearbyVehicleDto
{
    public int VehicleId { get; set; }
    public int CourierId { get; set; }
    public int VehicleType { get; set; }
    public string VehicleTypeName { get; set; } = "";
    public string LicensePlate { get; set; } = "";
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string CourierName { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceKm { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? WorkStartTime { get; set; }
    public string? WorkEndTime { get; set; }
    public bool IsWithinWorkingHours { get; set; } = true;
    public string? ServiceAreasSummary { get; set; }
    /// <summary>Bu konum kaydı alt kuryeye mi ait (ParentCourierId set).</summary>
    public bool IsSubCourier { get; set; }
}
