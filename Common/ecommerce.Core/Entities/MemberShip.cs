using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities
{
	public class Membership:IEntity<int>
	{
        public int Id { get; set; }
        /// <summary>
        /// 1:Eczane
        /// 2:Ecza Deposu
        /// </summary>
        public UserType UserType { get; set; }
        public CompanyWorkingType CompanyWorkingType { get; set; }
        public int? PharmacyTypeId { get; set; }
        //[ForeignKey("PharmacyTypeId")]
        public PharmacyType? PharmacyType { get; set; }
        public string ? PharmacyName{get;set;}
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? GlnNumber { get; set; }
        public string EmailAddress { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public int CityId { get; set; } 
        public int TownId { get; set; }
        public string ? Iban{get;set;} = "TR";
        public string ? BankAccountName{get;set;}
        public string? AccountName { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxName { get; set; }
        public string? AccountEmailAddress { get; set; }
        public string? InvoiceAddress { get; set; }
        public bool SendEmail { get; set; }
        public string? FileDocumenturl { get; set; }
        public DateTime RegisterDate { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsKvkkConfirmed{get;set;}
        public bool IsBuyer{get;set;}
       

    }
}

