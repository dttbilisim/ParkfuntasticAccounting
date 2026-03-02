using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CurrencyData:AuditableEntity<int>{


    public string Code{get;set;}
    public decimal Price{get;set;}
    public decimal OldPrice{get;set;}
    public int? Order{get;set;}
    public string ? Description{get;set;}
   
}
