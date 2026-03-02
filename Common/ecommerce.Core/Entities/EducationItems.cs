using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class EducationItems:AuditableEntity<int>{
    public int EducationId{get;set;}
    public string Name{get;set;}
    public string ? SubText{get;set;}
    public string ? Description{get;set;}
    public int Order {get;set;}
    public string? Url { get; set; }
    public int? Duration { get; set; }

    [ForeignKey("EducationId")]
    public Education Education { get; set; }
    
    public ICollection<EducationImages>  EducationImages { get; set; }
}
