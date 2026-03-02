using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

/// <summary>
/// Kuryenin hizmet verdiği bölge: il + ilçe + isteğe bağlı mahalle.
/// Aynı (CourierId, CityId, TownId, NeighboorId) tekrar eklenemez.
/// </summary>
public class CourierServiceArea
{
    public int Id { get; set; }

    [ForeignKey(nameof(CourierId))]
    public Courier? Courier { get; set; }
    public int CourierId { get; set; }

    /// <summary>Bu bölge hangi araçla hizmet verilecek. Null = eski kayıt (araç atanmamış).</summary>
    [ForeignKey(nameof(CourierVehicleId))]
    public CourierVehicle? CourierVehicle { get; set; }
    public int? CourierVehicleId { get; set; }

    [ForeignKey(nameof(CityId))]
    public City? City { get; set; }
    public int CityId { get; set; }

    [ForeignKey(nameof(TownId))]
    public Town? Town { get; set; }
    public int TownId { get; set; }

    [ForeignKey(nameof(NeighboorId))]
    public Neighboor? Neighboor { get; set; }
    public int? NeighboorId { get; set; }

    /// <summary>Bölge bazlı çalışma saati başlangıç (örn. 09:00). Null = tüm gün / belirsiz.</summary>
    [Column("workstarttime")]
    public TimeOnly? WorkStartTime { get; set; }

    /// <summary>Bölge bazlı çalışma saati bitiş (örn. 18:00). Null = tüm gün / belirsiz.</summary>
    [Column("workendtime")]
    public TimeOnly? WorkEndTime { get; set; }

    /// <summary>Bölge aktif mi; pasif bölgeler teslimat atamasında kullanılmaz.</summary>
    public bool IsActive { get; set; } = true;
}
