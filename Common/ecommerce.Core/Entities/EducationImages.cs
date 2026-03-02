using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class EducationImages:AuditableEntity<int>{
    public int EducationItemId{get;set;}
    public string ItemUrl{get;set;}
    public int Order {get;set;}
    

    [ForeignKey("EducationItemId")]
    public EducationItems EducationItems { get; set; }
    

}
