using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotConstructionPeriods")]
public class DotConstructionPeriod
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string DatECode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string ConstructionTimeMin { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string ConstructionTimeMax { get; set; } = string.Empty;
    
    /// <summary>
    /// Aktuelle Preisliste veya en güncel constructionTime değeri
    /// </summary>
    [StringLength(20)]
    public string? CurrentConstructionTime { get; set; }
    
    // Türev alanlar: yıl karşılıkları (ay dikkate alınmadan, yaklaşık)
    public int? YearMin { get; set; }
    public int? YearMax { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUpdatedDate { get; set; }
}

