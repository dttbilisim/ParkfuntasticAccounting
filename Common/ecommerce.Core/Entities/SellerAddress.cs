using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

public class SellerAddress : AuditableEntity<int>
{
    [ForeignKey("SellerId")]
    public virtual Seller Seller { get; set; } = null!;
    public int SellerId { get; set; }

    [ForeignKey("CityId")]
    public virtual City City { get; set; } = null!;
    public int CityId { get; set; }

    [ForeignKey("TownId")]
    public virtual Town Town { get; set; } = null!;
    public int TownId { get; set; }

    public string Address { get; set; } = null!;

    public string? Email { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? Title { get; set; }
    
    public string? StockWhereIs { get; set; }

    public bool IsDefault { get; set; }
}
