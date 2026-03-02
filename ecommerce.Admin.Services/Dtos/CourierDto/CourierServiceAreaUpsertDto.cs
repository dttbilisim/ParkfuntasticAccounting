namespace ecommerce.Admin.Domain.Dtos.CourierDto;

public class CourierServiceAreaUpsertDto
{
    public int? CourierVehicleId { get; set; }
    public int CityId { get; set; }
    public int TownId { get; set; }
    public int? NeighboorId { get; set; }
    /// <summary>Çalışma saati başlangıç "HH:mm". Null = tüm gün.</summary>
    public string? WorkStartTime { get; set; }
    /// <summary>Çalışma saati bitiş "HH:mm". Null = tüm gün.</summary>
    public string? WorkEndTime { get; set; }
    public bool IsActive { get; set; } = true;
}
