using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotTokenCaches")]
public class DotTokenCache
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsedDate { get; set; }
    
    public bool IsActive { get; set; } = true;
}
