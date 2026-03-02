using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CourierDto;

/// <summary>Sepet/checkout için teslimat seçeneği: kargo veya kurye (il/ilçe uygunsa).</summary>
public class CourierDeliveryOptionDto
{
    public DeliveryOptionType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int? EstimatedMinutes { get; set; }
    public decimal? Price { get; set; }
    /// <summary>Kurye seçeneği için atanabilir kurye id (opsiyonel; seçildiğinde order'a atanır).</summary>
    public int? CourierId { get; set; }
    /// <summary>Kurye adı (checkout listesinde gösterilir).</summary>
    public string? CourierName { get; set; }
    /// <summary>Araç tipi (Motosiklet, Bisiklet vb.).</summary>
    public string? VehicleTypeName { get; set; }
    /// <summary>Kullanıcıya uzaklık (km). Konum gönderilirse dolu.</summary>
    public double? DistanceKm { get; set; }
}
