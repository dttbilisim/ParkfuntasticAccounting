using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto;
public class CompanyInterviewDto{
    public int Id { get; set; }

    public int CompanyId{get;set;}
    public DateTime Created{get;set;}
    public DateTime? Updated{get;set;}
    public string Message{get;set;}
    public string InterviewPersonel{get;set;}
    
    public Company Company{get;set;}
}
