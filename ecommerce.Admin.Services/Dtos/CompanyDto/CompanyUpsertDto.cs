using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto
{
    [AutoMap(typeof(Company), ReverseMap = true)]
    public class CompanyUpsertDto
    {
        public int? Id { get; set; }
        public CompanyWorkingType CompanyWorkingType { get; set; }
        public UserType UserType { get; set; }
        public int? PharmacyTypeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Description { get; set; }
        public int? VendorPaymentTransferTime { get; set; }

        public DateTime? BirthDate { get; set; }
        public string ? SellerNotes{get;set;}
        public string? GlnNumber { get; set; }
        public string EmailAddress { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public int CityId { get; set; }
        public int TownId { get; set; }
        public string? Iban { get; set; }
        public string ? BankAccountName{get;set;}
        public string ? PharmacyName{get;set;}
        public string? AccountName { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxName { get; set; }
        public string? AccountEmailAddress { get; set; }
        public string? InvoiceAddress { get; set; }
        public string? IyzicoSubmerhantKey { get; set; }
        public decimal? MinBasketAmount { get; set; }
        public decimal? MinCartTotal{get;set;}
        public int? Rate { get; set; }
        public string? FileDocumenturl { get; set; }
        public int Status { get; set; }
        public bool IsLocalStorage{get;set;}
        
        // Ilac firmalari video ekleme limitleri
        public int OnlineVideoLimit{get;set;} = 0;
        public int OfflineVideoLimit{get;set;} = 0;
        public bool IsBlueTick{get;set;}
        
        public string ? SipaySubmerchantKey{get;set;}
        public int ? SipayPaymentDay{get;set;}
        public int ? SipayCommission{get;set;}
        public string ? EntegraXmlLink{get;set;}
        public string ? BizimHesapXmlLink{get;set;}
        public string ? ParasutXmlLink{get;set;}
        public string ? BiFaturaXmlLink{get;set;}
        public string ? ProPazarXmlLink{get;set;}
        public string ? EntegraOrderXml{get;set;}

        [Ignore]
        public bool StatusBool { get; set; }

        [Ignore]
        public string Password { get; set; }

    }
}
