using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class PharmacyData : AuditableEntity<int>{
    public string ? PharmacyType{get;set;}
    public string ? GlnNumber{get;set;}
    public string ? PharmacyName{get;set;}
    public string ? Email{get;set;}
    public string ? StatusText{get;set;}
    public string ? City{get;set;}
    public string ? Town{get;set;}
 
}
