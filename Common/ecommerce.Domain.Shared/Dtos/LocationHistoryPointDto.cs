namespace ecommerce.Domain.Shared.Dtos;

/// <summary>
/// Plasiyer konum geçmişi noktası — Redis list'ten deserialize edilir
/// </summary>
public class LocationHistoryPointDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    /// <summary>
    /// Unix timestamp (saniye)
    /// </summary>
    public long Ts { get; set; }
}
