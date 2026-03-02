using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class AdvertOther:AuditableEntity<int>{


    public int AdvertId{get;set;}
    public int CompanyId{get;set;}
    
    public decimal Price{get;set;}
    public decimal Stock{get;set;}
    public DateTime? Expiration{get;set;}
    public string ? Description{get;set;}
    public bool IsFuture{get;set;}
    public bool IsBestPrice{get;set;}
    public bool IsBasket{get;set;}
  
    
    //product
    
    public int ProductId{get;set;}
    public string ? ProductName{get;set;}
    public string ? ImageUrl{get;set;}
    public decimal? MinPrice{get;set;}
    public string? Brand{get;set;}
    public string ? Category{get;set;}
    
    
    [ForeignKey("CompanyId")] public Company Company{get;set;}
    
    
}
