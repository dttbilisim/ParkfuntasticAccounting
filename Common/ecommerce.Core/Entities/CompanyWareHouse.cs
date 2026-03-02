using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CompanyWareHouse: AuditableEntity<int>{
    
    public int CompanyId{get;set;}
    public int CityId{get;set;}
    public int TownId{get;set;}
    
    
    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;
    
    
    
    
    
    
  
}
