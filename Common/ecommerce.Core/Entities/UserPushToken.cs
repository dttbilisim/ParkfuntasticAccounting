using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;

namespace ecommerce.Core.Entities;

/// <summary>
/// Kullanıcının push bildirim token'ını saklayan entity.
/// Mobil (Expo push token) ve web (subscription JSON) platformlarını destekler.
/// </summary>
public class UserPushToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Kullanıcı FK</summary>
    public int UserId { get; set; }

    /// <summary>Expo push token veya web subscription JSON</summary>
    [Required]
    public string Token { get; set; } = null!;

    /// <summary>Platform bilgisi: "ios", "android", "web"</summary>
    [Required]
    [MaxLength(10)]
    public string Platform { get; set; } = null!;

    /// <summary>Cihaz benzersiz tanımlayıcı</summary>
    [Required]
    public string DeviceId { get; set; } = null!;

    /// <summary>Token oluşturulma tarihi</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Token son güncellenme tarihi</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Token aktif mi? 30 günden eski token'lar pasif işaretlenir</summary>
    public bool IsActive { get; set; } = true;

    // Navigation property
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}
