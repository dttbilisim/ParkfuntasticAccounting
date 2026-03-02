using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.MembershipDto
{
	
        [AutoMap(typeof(Membership), ReverseMap = true)]
        public class MembershipUpsertDto
        {
        public UserType UserType { get; set; }
        public int? Id { get; set; }

        public int PharmacyTypeId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime? BirthDate { get; set; }
        public string? GlnNumber { get; set; }
        public string EmailAddress { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public int CityId { get; set; }
        public int TownId { get; set; }
        public string? Iban { get; set; }
        public string ? BankAccountName{get;set;}
        public string? AccountName { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxName { get; set; }
        public string? AccountEmailAddress { get; set; }
        public string? InvoiceAddress { get; set; }
        public DateTime RegisterDate { get; set; } 
        public string? FileDocumenturl { get; set; }
        public bool SendEmail { get; set; }
        public CompanyWorkingType CompanyWorkingType { get; set; }
    }
    
}

