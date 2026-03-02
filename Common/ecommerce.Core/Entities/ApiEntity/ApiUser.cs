using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ecommerce.Core.ApiEntity;
public class ApiUser{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Username{get;set;}
    public string Password{get;set;}
    
    
}
