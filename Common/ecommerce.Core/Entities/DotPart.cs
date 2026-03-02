using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotParts")]
public class DotPart
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string PartNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Name { get; set; }
    
    [StringLength(100)]
    public string? Brand { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NetPrice { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? GrossPrice { get; set; }
    
    [StringLength(10)]
    public string? Currency { get; set; }
    
    [StringLength(50)]
    public string? Availability { get; set; }

    public DateTime? PriceDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WorkTimeMin { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WorkTimeMax { get; set; }

    [StringLength(20)]
    public string? DatProcessNumber { get; set; }

    public int? VehicleType { get; set; }

    [StringLength(150)]
    public string? VehicleTypeName { get; set; }

    [StringLength(20)]
    public string? ManufacturerKey { get; set; }

    [StringLength(150)]
    public string? ManufacturerName { get; set; }

    [StringLength(20)]
    public string? BaseModelKey { get; set; }

    [StringLength(200)]
    public string? BaseModelName { get; set; }

    [StringLength(20)]
    public string? DescriptionIdentifier { get; set; }

    [Column(TypeName = "text")]
    public string? SubModelsJson { get; set; }

    [Column(TypeName = "text")]
    public string? PreviousPricesJson { get; set; }

    [Column(TypeName = "text")]
    public string? PreviousPartNumbersJson { get; set; }
    
    [StringLength(50)]
    public string? DatVehicleId { get; set; }
    
    public int? DotVehicleId { get; set; }
    
    [ForeignKey("DotVehicleId")]
    public virtual DotVehicle? DotVehicle { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSyncDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}
