using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

/// <summary>
/// Onaylanmış kurye. Başvuru (CourierApplication) onaylandığında oluşturulur.
/// </summary>
public class Courier : AuditableEntity<int>
{
    [ForeignKey(nameof(ApplicationUserId))]
    public ApplicationUser? ApplicationUser { get; set; }
    public int ApplicationUserId { get; set; }

    /// <summary>EntityStatus.Active/Passive; kurye aktif mi.</summary>
    public virtual ICollection<CourierServiceArea> ServiceAreas { get; set; } = new List<CourierServiceArea>();
    /// <summary>Kuryenin kayıtlı araçları (Motosiklet, Bisiklet, Otomobil, Kamyonet + plaka).</summary>
    public virtual ICollection<CourierVehicle> Vehicles { get; set; } = new List<CourierVehicle>();
}
