namespace ecommerce.Admin.Domain.Dtos.CourierDto;

/// <summary>B2B/B2C kullanıcılar için yakındaki kurye listesi (harita modal).</summary>
public class NearbyCourierDto
{
    public int CourierId { get; set; }
    public string CourierName { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    /// <summary>Kullanıcı konumuna uzaklık (km).</summary>
    public double DistanceKm { get; set; }
    public DateTime RecordedAt { get; set; }
    /// <summary>Çalışma saati başlangıç "HH:mm". Null = tanımsız/tüm gün.</summary>
    public string? WorkStartTime { get; set; }
    /// <summary>Çalışma saati bitiş "HH:mm". Null = tanımsız/tüm gün.</summary>
    public string? WorkEndTime { get; set; }
    /// <summary>Şu an kurye çalışma saatleri içinde mi?</summary>
    public bool IsWithinWorkingHours { get; set; } = true;
    /// <summary>Hizmet bölgeleri özeti (örn. "Konyaaltı / Antalya" veya "Kadıköy / İstanbul, Üsküdar / İstanbul").</summary>
    public string? ServiceAreasSummary { get; set; }
    /// <summary>Alt kurye mi (ParentCourierId set ise true).</summary>
    public bool IsSubCourier { get; set; }
}
