using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class MyFavorites : AuditableEntity<int>{
   
    public int UserId{get;set;}
    public int ProductId{get;set;}
    
    [ForeignKey("ProductId")] public Product Product{get;set;}
    [ForeignKey("UserId")] public User User{get;set;}
}
