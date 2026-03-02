using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
namespace ecommerce.Core.Entities;
public class EducationCalendar: AuditableEntity<int>{
    
    public int CompanyId{get;set;} = 0;
    public UserType? UserTypeId{get;set;} = 0;
    public int? PharmacyTypeId{get;set;} = 0;
   
    public string Name { get; set; }
    public string ? Description{get;set;}
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Color { get; set; }
    public string Url{get;set;} = "#";
   
}
