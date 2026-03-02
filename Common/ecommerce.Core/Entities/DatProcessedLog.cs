using System.ComponentModel.DataAnnotations;

namespace ecommerce.Core.Entities;

/// <summary>
/// Her entity type için TEK kayıt tutar, incremental sync destekler
/// </summary>
public class DatProcessedLog
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Entity tipi: VehicleTypes, Manufacturers, BaseModels, SubModels, Parts, etc.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Son işlenen entity'nin unique key'i (incremental sync için)
    /// Örn: "Manufacturers-2-425" → son işlenen VehicleType=2, Manufacturer=425
    /// </summary>
    [StringLength(200)]
    public string? LastProcessedKey { get; set; }
    
    /// <summary>
    /// Son işlenen entity'nin DB ID'si (varsa)
    /// </summary>
    public int? LastProcessedEntityId { get; set; }
    
    /// <summary>
    /// Bu entity type için toplam kaç kayıt işlendi
    /// </summary>
    public int TotalProcessed { get; set; } = 0;
    
    /// <summary>
    /// İlk sync zamanı
    /// </summary>
    public DateTime FirstSyncDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Son sync zamanı
    /// </summary>
    public DateTime LastSyncDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Aktif mi (soft delete için)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
