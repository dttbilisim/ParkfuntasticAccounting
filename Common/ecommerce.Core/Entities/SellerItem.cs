using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class SellerItem:AuditableEntity<int>{
    
    [ForeignKey("SellerId")] public Seller Seller{get;set;}
    public int SellerId{get;set;}
    [ForeignKey("ProductId")] public Product Product{get;set;}
    public int ProductId{get;set;}
    
    public decimal Stock {get;set;}
    public decimal CostPrice{get;set;}
    public decimal SalePrice{get;set;}
    public int Commision{get;set;}
    public string Currency{get;set;}
    public string Unit{get;set;}
    public string? SourceId { get; set; }
    public string? ManufacturerName { get; set; }  // Seller-specific brand name
    
    public decimal MinSaleAmount { get; set; } = 1;
    public decimal MaxSaleAmount { get; set; } = 0;
    public decimal Step { get; set; } = 1;
    
    
}
