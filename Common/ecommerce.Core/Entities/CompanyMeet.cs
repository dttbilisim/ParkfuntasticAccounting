using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CompanyMeet:AuditableEntity<int>{
    public int Id{get;set;}
    public int CompanyId{get;set;}
    public DateTime MeetDate{get;set;}
    public int MeetCount{get;set;}
    public int MeetCounUse{get;set;}
    
    [ForeignKey("CompanyId")] public Company Company{get;set;}
    
}
