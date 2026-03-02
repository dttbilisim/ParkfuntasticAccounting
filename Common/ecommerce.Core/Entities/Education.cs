using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities;
public class Education: AuditableEntity<int>{
   
    public string Name{get;set;}
    public string ? SubText{get;set;}
    public string ? Description{get;set;}
    public int CategoryId{get;set;}
    public string ? PubliserName{get;set;}
    public string ? ImageUrl{get;set;}
    public string ? ButtonText{get;set;}
    public string ? ButtonUrl{get;set;}
    public int Order {get;set;}
    public DateTime StartDate{get;set;}
    public DateTime EndDate{get;set;}

    public bool IsSlider { get; set; }
    public EducationCategoryType EducationCategoryType { get; set; }
    
    [ForeignKey("CategoryId")]
    public EducationCategory EducationCategory { get; set; }

    public ICollection<EducationItems> EducationItems { get; set; }
}
