using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class Notification:AuditableEntity<int>{

    public int CompanyId{get;set;}
    public string Description{get;set;}
    public bool IsRead{get;set;}
    public string ? ProcessCode{get;set;}
    [ForeignKey("CompanyId")] public Company Company{get;set;}
    
}
