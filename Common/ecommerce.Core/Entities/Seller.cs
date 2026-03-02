using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class Seller:AuditableEntity<int>{
    public string Name{get;set;}
    public string ? Email{get;set;}
    public string ? Address { get; set; }
    public string ? PhoneNumber { get; set; }
    public decimal Commission{get;set;}
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public bool IsOrderUse { get; set; }
    public DateTime? SyncDate { get; set; }
    public string? SyncMessage { get; set; }
    public int? BranchId { get; set; }
    
    [ForeignKey("CityId")] public City City { get; set; }
    public int? CityId { get; set; }
   
    [ForeignKey("TownId")] public Town Town { get; set; }
    public int? TownId { get; set; }

    public virtual ICollection<CompanyCargo> CompanyCargos { get; set; } = new List<CompanyCargo>();
    
    public virtual ICollection<SellerAddress> SellerAddresses { get; set; } = new List<SellerAddress>();
    
}
