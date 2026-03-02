using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class CompanyInterview:IEntity<int>{
    public int Id{get;set;}
    public int CompanyId{get;set;}
    public DateTime Created{get;set;}
    public DateTime? Updated{get;set;}
    public string Message{get;set;}
    public string InterviewPersonel{get;set;}
    
    [ForeignKey("CompanyId")] public Company Company{get;set;}
}
