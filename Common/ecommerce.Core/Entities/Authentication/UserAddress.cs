using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities.Authentication;
public class UserAddress:AuditableEntity<int>{

    // Web context için User (UserId FK ile)
    [ForeignKey("UserId")] 
    public User? User{get;set;}
    public int? UserId{get;set;}
    
    // Admin context için ApplicationUser (ApplicationUserId FK ile)
    [ForeignKey("ApplicationUserId")] 
    public ApplicationUser? ApplicationUser{get;set;}
    public int? ApplicationUserId{get;set;}

    public string AddressName{get;set;}
    
    public string FullName{get;set;}
    public string Email{get;set;}
    public string PhoneNumber{get;set;}
    public string Address{get;set;}
    
    // City
    [ForeignKey("CityId")] public City City { get; set; }
    public int? CityId { get; set; }
   
    
    [ForeignKey("TownId")] public Town Town { get; set; }
    public int? TownId { get; set; }

    public string? IdentityNumber { get; set; } // TC Kimlik Numarası
    public bool IsDefault { get; set; } = false; // Varsayılan adres mi?
    
    // Invoice Address Fields
    public bool IsSameAsDeliveryAddress { get; set; } = true; // Fatura adresi teslimat adresi ile aynı mı?
    
    [ForeignKey("InvoiceCityId")] public City? InvoiceCity { get; set; }
    public int? InvoiceCityId { get; set; }
    
    [ForeignKey("InvoiceTownId")] public Town? InvoiceTown { get; set; }
    public int? InvoiceTownId { get; set; }
    
    public string? InvoiceAddress { get; set; } // Fatura Adres Detayı
}
