using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

/// <summary>
/// Kurye konum kaydı. Canlı takip için kurye periyodik konum gönderir; müşteri son konumu çeker.
/// </summary>
public class CourierLocation
{
    public int Id { get; set; }

    [ForeignKey(nameof(CourierId))]
    public Courier? Courier { get; set; }
    public int CourierId { get; set; }

    /// <summary>Belirli sipariş için konum (null ise genel konum).</summary>
    [ForeignKey(nameof(OrderId))]
    public Orders? Order { get; set; }
    public int? OrderId { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    /// <summary>Metre cinsinden doğruluk (opsiyonel).</summary>
    public double? Accuracy { get; set; }

    /// <summary>Kayıt zamanı (UTC önerilir).</summary>
    public DateTime RecordedAt { get; set; }
}
