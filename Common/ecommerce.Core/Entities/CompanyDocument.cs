using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CompanyDocument: AuditableEntity<int>{
    
    public string Email{get;set;}
    public int FileId{get;set;}
    public string FileName{get;set;}
    public string Base64data{get;set;}
    public string Root{get;set;}
    public string ContentType { get; set; }
    

}
