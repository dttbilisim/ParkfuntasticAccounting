using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotSubModels")]
public class DotSubModel
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string DatKey { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public int VehicleType { get; set; }
    
    [Required]
    [StringLength(50)]
    public string ManufacturerKey { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string BaseModelKey { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSyncDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}

