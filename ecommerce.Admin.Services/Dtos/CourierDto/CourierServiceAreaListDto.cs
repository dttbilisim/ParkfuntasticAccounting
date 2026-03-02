namespace ecommerce.Admin.Domain.Dtos.CourierDto;

public class CourierServiceAreaListDto
{
    public int Id { get; set; }
    public int CourierId { get; set; }
    /// <summary>Kurye adı (liste ekranında gösterilir).</summary>
    public string? CourierName { get; set; }
    public int? CourierVehicleId { get; set; }
    public string? VehicleDisplay { get; set; }
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int TownId { get; set; }
    public string TownName { get; set; } = string.Empty;
    public int? NeighboorId { get; set; }
    public string? NeighboorName { get; set; }
    /// <summary>Çalışma saati başlangıç "HH:mm" (örn. 09:00). Null = tüm gün.</summary>
    public string? WorkStartTime { get; set; }
    /// <summary>Çalışma saati bitiş "HH:mm" (örn. 18:00). Null = tüm gün.</summary>
    public string? WorkEndTime { get; set; }
    public bool IsActive { get; set; } = true;
}
