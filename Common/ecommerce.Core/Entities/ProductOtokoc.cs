using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class ProductOtokoc:AuditableEntity<int>{
    [MaxLength(50)]
    public string ProductCode { get; set; }

    [MaxLength(500)]
    public string ProductName { get; set; }

    [MaxLength(50)]
    public string BrandCode { get; set; }

    [MaxLength(200)]
    public string BrandName { get; set; }

    public decimal? Price { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; }

    public int? StockQuantity { get; set; }

    public decimal? TaxRate { get; set; }

    [MaxLength(200)]
    public string Barcode { get; set; }

    [MaxLength(200)]
    public string OEM { get; set; }

    [MaxLength(50)]
    public string GroupCode { get; set; }

    [MaxLength(200)]
    public string GroupName { get; set; }

    [MaxLength(500)]
    public string? ImagePath { get; set; }
}
