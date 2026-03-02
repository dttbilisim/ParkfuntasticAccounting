using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Identity;
namespace ecommerce.Core.Entities.Authentication;
public class User:IdentityUser<int> {


    public WebUserType WebUserType{get;set;}
    public string ? FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string ? LastName { get; set; } = null!;

    public string? VatNumber { get; set; } // Vergi Numarası
    public string?  VatOffice { get; set; } // Vergi Dairesi
    public DateTime? RegisterDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime? BirthDate { get; set; }

    public string ? CompanyName{get;set;}
    
    [NotMapped]
    public string FullName
    {
        get { return FirstName + " " + LastName; }
    }
    public List<ApplicationRole> Roles { get; set; } = new();
   
    public string? NewUserToken { get; set; }
    public bool IsAproved{get;set;}

    public string? ResetEmailToken { get; set; }
    public DateTime? ResetEmailTokenExpireDate { get; set; }
    public string? FileDocumenturl { get; set; }
    
    // Multi-address support for B2C users
    public virtual List<UserAddress> UserAddresses { get; set; } = new();
}
