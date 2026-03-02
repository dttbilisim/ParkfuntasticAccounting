using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotVehicles")]
public class DotVehicle
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string DatId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(300)]
    public string Make { get; set; } = string.Empty;
    
    [Required]
    [StringLength(300)]
    public string Model { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? Year { get; set; }
    
    [StringLength(200)]
    public string? Engine { get; set; }
    
    [StringLength(50)]
    public string? FuelType { get; set; }
    
    [Required]
    public int DotVehicleTypeId { get; set; }
    
    [ForeignKey("DotVehicleTypeId")]
    public virtual DotVehicleType DotVehicleType { get; set; } = null!;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSyncDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}
