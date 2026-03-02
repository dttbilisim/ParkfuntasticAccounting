using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities
{
    public class Company : AuditableEntity<int>
    {
        public CompanyWorkingType CompanyWorkingType { get; set; }
        public UserType UserType { get; set; }
        public int? PharmacyTypeId { get; set; }
       
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? GlnNumber { get; set; }
        public string EmailAddress { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public string ? Description { get; set; }
        public int? VendorPaymentTransferTime { get; set; }

        public int CityId { get; set; }
      
        public int TownId { get; set; }
        public string ? SellerNotes{get;set;}
      
        public string? Iban { get; set; }
        public string ? BankAccountName{get;set;}
        public string ? PharmacyName{get;set;}
        public string? AccountName { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxName { get; set; }
        public string? AccountEmailAddress { get; set; }
        public string? InvoiceAddress { get; set; }
        public string? FileDocumenturl { get; set; }
        public string? ProfilPhotoUrl { get; set; }
        public string? ProfilBackgroungPhotoUrl { get; set; }
        public string? ProfileVideoUrl { get; set; }
        public int? Rate { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsKvkkConfirmed{get;set;}

        public bool? IsEmailConfirmed{get;set;}
        public bool? IsSmsConfirmed{get;set;}
      
        public string? EmailAcceptList { get; set; }
        public string? SmsAcceptList { get; set; }
        public decimal MinCartTotal { get; set; } = 0;
        public decimal? MinBasketAmount { get; set; }
        public string? IyzicoSubmerhantKey { get; set; }
        public int? FkId{get;set;}
        public Guid CompanyKey{get;set;} = Guid.NewGuid();
       
        
        //satici komisyonu aktarilma suresi iyzico icin
        public int IyzicoPaymentDay{get;set;} = 0;
        public decimal? CargoPrice { get; set; } = 0;
        public decimal SellerPoint{get;set;} = 0;
        public int SellerSalesCount{get;set;} = 0;
        
        // Ilac firmalari video ekleme limitleri
        public int OnlineVideoLimit{get;set;} = 0;
        public int OfflineVideoLimit{get;set;} = 0;

        public bool IsLocalStorage{get;set;}
        public string ? EmailSecond{get;set;}
        public string ? PhoneNumberSecond{get;set;}
        public bool IsBlueTick{get;set;} = false;

        
        //sipay prop

        public string ? SipaySubmerchantKey{get;set;}
        public int ? SipayPaymentDay{get;set;} = 0;
        public int ? SipayCommission{get;set;} = 0;

        public string ? EntegraXmlLink{get;set;}
        public string ? BizimHesapXmlLink{get;set;}
        public string ? ParasutXmlLink{get;set;}
        public string ? BiFaturaXmlLink{get;set;}
        public string ? ProPazarXmlLink{get;set;}

        public string ? EntegraOrderXml{get;set;}

       
        
        [ForeignKey("UserId")] public User ? User{get;set;}
        
    }
}

