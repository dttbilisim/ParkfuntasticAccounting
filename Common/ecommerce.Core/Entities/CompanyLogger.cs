using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CompanyLogger: AuditableEntity<int>{

    public int CompanyId{get;set;}
    public string LogName{get;set;}
    

    
    
}
