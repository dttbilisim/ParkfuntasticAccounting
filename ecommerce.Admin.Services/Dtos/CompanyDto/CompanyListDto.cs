using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto
{
    [AutoMap(typeof(Company))]
    public class CompanyListDto
    {
        public int Id { get; set; }

        public string IdStr
        {
            get
            {
                return Id.ToString();
            }
        }
        public CompanyWorkingType CompanyWorkingType { get; set; }
        public UserType UserType { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? GlnNumber { get; set; }
        public int CityId { get; set; }
        public int TownId { get; set; }
        public DateTime RegisterDate { get; set; }
        public EntityStatus Status { get; set; }
        public string IyzicoSubmerhantKey { get; set; }
        public string ? SellerNotes{get;set;}
        public string EmailAddress{get;set;}
        public string AccountName{get;set;}
        public string ? BankAccountName{get;set;}
        public string ? SipaySubmerchantKey{get;set;}
        public string ? PharmacyName{get;set;}
        public string ? PhoneNumber{get;set;}
        public string ? PhoneNumberSecond{get;set;}
        public string ? Address{get;set;}
        public string? TaxNumber { get; set; }
        public string? TaxName { get; set; }
        public int ? SipayPaymentDay{get;set;}
        public int ? SipayCommission{get;set;}
        public string ? EntegraXmlLink{get;set;}
        public string ? BizimHesapXmlLink{get;set;}
        public string ? ParasutXmlLink{get;set;}
        public string ? BiFaturaXmlLink{get;set;}
        public string ? ProPazarXmlLink{get;set;}

        public City City { get; set; }
        public Town Town { get; set; }
        public string FullName => $"{FirstName} {LastName} - ( {AccountName} - ( {EmailAddress} ) )";

      //  [Ignore]
        public string CompanyInformation
        {
            get
            {
                return FirstName!=null?$"{FirstName} {LastName}": AccountName ;
            }
        }
       
    }
}
