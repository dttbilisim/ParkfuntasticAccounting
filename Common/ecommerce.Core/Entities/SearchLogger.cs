using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class SearchLogger:AuditableEntity<int>{

    public string Name{get;set;}
    public int Count{get;set;}
    public int FoundProductCount{get;set;}
    
}
