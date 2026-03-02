using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotVehicleImages")]
public class DotVehicleImage
{
    [Key]
    public int Id { get; set; }
    
    // DatECode ile arama için (optional)
    [StringLength(50)]
    public string? DatECode { get; set; }
    
    // VehicleType, Manufacturer, BaseModel, SubModel ile arama için
    public int? VehicleType { get; set; }
    
    [StringLength(20)]
    public string? ManufacturerKey { get; set; }
    
    [StringLength(20)]
    public string? BaseModelKey { get; set; }
    
    [StringLength(20)]
    public string? SubModelKey { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Aspect { get; set; } = string.Empty; // SIDEVIEW, ANGULARFRONT, etc.
    
    [Required]
    [StringLength(20)]
    public string ImageType { get; set; } = string.Empty; // PICTURE
    
    [Required]
    [StringLength(10)]
    public string ImageFormat { get; set; } = string.Empty; // JPG, PNG
    
    [Required]
    public string ImageBase64 { get; set; } = string.Empty; // Base64 encoded image
    
    [StringLength(500)]
    public string? Url { get; set; } //
    
    public DateTime LastSyncDate { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}

