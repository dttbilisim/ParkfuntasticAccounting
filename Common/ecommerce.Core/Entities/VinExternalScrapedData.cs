using ecommerce.Core.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("VinExternalScrapedData")]
public class VinExternalScrapedData
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(17)]
    public string Vin { get; set; } = string.Empty;

    public string? Brand { get; set; }
    
    public string? FactoryCode { get; set; }
    
    public string? ProductionYear { get; set; }
    
    public string? ProductionPlace { get; set; }
    
    public string? ManufacturerCode { get; set; }
    
    public string? ModelCode { get; set; }
    
    public string? VehicleCode { get; set; }

    public DateTime ScrapeDate { get; set; } = DateTime.UtcNow;
}
