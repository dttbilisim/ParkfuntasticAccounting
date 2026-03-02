using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities;

/// <summary>
/// Kuryenin teslimat için kullandığı araç. Bir kuryenin birden fazla aracı olabilir.
/// Hizmet bölgeleri araç bazlı tanımlanır (CourierServiceArea.CourierVehicleId).
/// </summary>
public class CourierVehicle
{
    public int Id { get; set; }

    [ForeignKey(nameof(CourierId))]
    public Courier? Courier { get; set; }
    public int CourierId { get; set; }

    public CourierVehicleType VehicleType { get; set; }
    /// <summary>Plaka (örn. 34 ABC 123).</summary>
    public string LicensePlate { get; set; } = string.Empty;
    /// <summary>Şoför adı soyadı (tek alan). DriverUserId set ise bu alan ilgili kullanıcıdan doldurulur.</summary>
    public string? DriverName { get; set; }
    /// <summary>Şoför cep telefonu. DriverUserId set ise bu alan ilgili kullanıcıdan doldurulur.</summary>
    public string? DriverPhone { get; set; }
    /// <summary>Aracı kullanan alt kullanıcı (ana kuryenin eklediği). Set ise DriverName/DriverPhone bu kullanıcıdan gelir.</summary>
    public int? DriverUserId { get; set; }
    [ForeignKey(nameof(DriverUserId))]
    public virtual ecommerce.Core.Entities.Authentication.ApplicationUser? DriverUser { get; set; }

    public virtual ICollection<CourierServiceArea> ServiceAreas { get; set; } = new List<CourierServiceArea>();
}
