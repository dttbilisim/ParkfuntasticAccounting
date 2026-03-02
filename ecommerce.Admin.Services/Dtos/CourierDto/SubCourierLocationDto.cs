namespace ecommerce.Admin.Domain.Dtos.CourierDto;

/// <summary>Ana kurye harita ekranı: Alt kurye bilgisi + son konum (varsa).</summary>
public class SubCourierLocationDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    /// <summary>Alt kuryenin Courier kaydı Id. Yoksa 0.</summary>
    public int CourierId { get; set; }
    /// <summary>Son konum enlem. Konum yoksa null.</summary>
    public double? Latitude { get; set; }
    /// <summary>Son konum boylam. Konum yoksa null.</summary>
    public double? Longitude { get; set; }
    /// <summary>Konum kaydı zamanı (UTC).</summary>
    public DateTime? RecordedAt { get; set; }
}
