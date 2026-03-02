using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.ApiEntity;
namespace ecommerce.Core.Entities.ApiEntity;
public class ApiCompany{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int ApiId{get;set;}
    public Guid CompanyKey{get;set;}
    
    [ForeignKey("ApiId")] 
    public ApiUser ApiUser{get;set;}
    
    [ForeignKey("CompanyId")]
    public Company Company{get;set;}
}
