using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotVehicleTypes")]
public class DotVehicleType
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string DatId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSyncDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}
