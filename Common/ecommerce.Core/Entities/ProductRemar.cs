using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class ProductRemar:AuditableEntity<int>{
    public string Code { get; set; }
    public string Cross_Referans { get; set; }
    public string Depo_1 { get; set; }
    public string Depo_2 { get; set; }
    public string Manufacturer { get; set; }
    public int MinOrderQuantity { get; set; }
    public string Name { get; set; }
    public string Oem_No { get; set; }
    public string PackageUsage { get; set; }
    public decimal SalePriceContact { get; set; }
    public string SalePriceContactCurrency { get; set; }
    public string SpecialField_1 { get; set; }
    public string SpecialField_2 { get; set; }
    public string Unit { get; set; }
}
