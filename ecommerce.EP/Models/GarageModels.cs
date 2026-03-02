namespace ecommerce.EP.Models;

/// <summary>
/// Araç ekleme isteği DTO'su
/// </summary>
public class AddUserCarRequest
{
    public int? DotVehicleTypeId { get; set; }
    public int? DotManufacturerId { get; set; }
    public int? DotBaseModelId { get; set; }
    public int? DotSubModelId { get; set; }
    public int? DotCarBodyOptionId { get; set; }
    public int? DotEngineOptionId { get; set; }
    public int? DotOptionId { get; set; }
    public string? DotManufacturerKey { get; set; }
    public string? DotBaseModelKey { get; set; }
    public string? DotSubModelKey { get; set; }
    public string? DotDatECode { get; set; }
    public string? PlateNumber { get; set; }
}

/// <summary>
/// Araç listesi yanıt DTO'su
/// </summary>
public class UserCarResponse
{
    public int Id { get; set; }
    public string? ManufacturerName { get; set; }
    public string? BaseModelName { get; set; }
    public string? SubModelName { get; set; }
    public string? ManufacturerLogoUrl { get; set; }
    public string? VehicleImageUrl { get; set; }
    public string? PlateNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    // Key bilgileri — düzenleme modunda eşleştirme için
    public int? DotVehicleTypeId { get; set; }
    public int? DotManufacturerId { get; set; }
    public int? DotBaseModelId { get; set; }
    public int? DotSubModelId { get; set; }
    public int? DotCarBodyOptionId { get; set; }
    public int? DotEngineOptionId { get; set; }
    public int? DotOptionId { get; set; }
    public string? DotManufacturerKey { get; set; }
    public string? DotBaseModelKey { get; set; }
    public string? DotSubModelKey { get; set; }
}
