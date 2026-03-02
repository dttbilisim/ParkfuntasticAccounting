using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;

namespace ecommerce.Core.Entities;

/// <summary>
/// Gönderilen push bildirimlerin log kaydını tutan entity.
/// Admin panelden gönderilen bildirimlerin istatistiklerini saklar.
/// </summary>
public class NotificationLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Bildirim başlığı</summary>
    [Required]
    public string Title { get; set; } = null!;

    /// <summary>Bildirim mesaj metni</summary>
    [Required]
    public string Body { get; set; } = null!;

    /// <summary>Bildirime tıklandığında yönlendirilecek deep link URL'si</summary>
    public string? DeepLink { get; set; }

    /// <summary>Hedef kitle: "all", "merchant", "salesman", "user:{id}"</summary>
    [Required]
    public string TargetAudience { get; set; } = null!;

    /// <summary>Bildirimi gönderen admin kullanıcı FK</summary>
    public int? SentByUserId { get; set; }

    /// <summary>Bildirim gönderim zamanı</summary>
    public DateTime SentAt { get; set; }

    /// <summary>Toplam hedef kullanıcı/cihaz sayısı</summary>
    public int TotalTargets { get; set; }

    /// <summary>Başarılı gönderim sayısı</summary>
    public int SuccessCount { get; set; }

    /// <summary>Başarısız gönderim sayısı</summary>
    public int FailureCount { get; set; }

    /// <summary>Hata detayları — JSON formatında</summary>
    public string? ErrorDetails { get; set; }

    // Navigation property
    [ForeignKey("SentByUserId")]
    public virtual ApplicationUser? SentByUser { get; set; }
}
