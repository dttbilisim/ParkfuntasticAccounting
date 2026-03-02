using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class Weather: AuditableEntity<int>{
    public string ? Icon{get;set;}
    public string Temp{get;set;} 
    public string ? City{get;set;}
    
}
