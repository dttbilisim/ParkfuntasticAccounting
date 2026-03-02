using System.ComponentModel.DataAnnotations;
namespace ecommerce.Core.Entities;
public class SubModel{
    [Required] [MaxLength(50)] public string Key{get;set;}
    [MaxLength(200)] public string Name{get;set;}
}
