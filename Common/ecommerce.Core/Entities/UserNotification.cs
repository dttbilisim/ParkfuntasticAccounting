using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;

namespace ecommerce.Core.Entities;

/// <summary>
/// Kullanıcıya özel bildirim kaydı.
/// Admin panelden gönderilen bildirimler her hedef kullanıcı için ayrı kayıt oluşturur.
/// Kullanıcı kendi bildirimlerini listeleyebilir, okundu işaretleyebilir ve silebilir.
/// </summary>
public class UserNotification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Bildirim sahibi kullanıcı FK</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>Bildirim başlığı</summary>
    [Required]
    public string Title { get; set; } = null!;

    /// <summary>Bildirim mesaj metni</summary>
    [Required]
    public string Body { get; set; } = null!;

    /// <summary>Deep link URL'si</summary>
    public string? DeepLink { get; set; }

    /// <summary>Okundu mu?</summary>
    public bool IsRead { get; set; }

    /// <summary>Silinmiş mi? (soft delete)</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Bildirim oluşturulma zamanı</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Okunma zamanı</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>İlişkili NotificationLog FK (opsiyonel)</summary>
    public int? NotificationLogId { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    [ForeignKey("NotificationLogId")]
    public virtual NotificationLog? NotificationLog { get; set; }
}
