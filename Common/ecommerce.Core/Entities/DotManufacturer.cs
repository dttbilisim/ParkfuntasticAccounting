using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotManufacturers")]
public class DotManufacturer
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string DatKey { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public int VehicleType { get; set; }
    
    [StringLength(500)]
    public string? LogoUrl { get; set; }
    
    public int Order { get; set; } = 999;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSyncDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}

