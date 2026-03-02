using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class Currencies: AuditableEntity<int>{

    public decimal UsdSeller{get;set;} = 0;
    public decimal UsdBuyer{get;set;}=0;
    public decimal EuroSeller{get;set;} = 0;
    public decimal EuroBuyer{get;set;} = 0;
    
    
    public decimal OldUsdSeller{get;set;} = 0;
    public decimal OldUsdBuyer{get;set;}=0;
    public decimal OldEuroSeller{get;set;} = 0;
    public decimal OldEuroBuyer{get;set;} = 0;

   

}
