using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities {
    public class CreditCard : AuditableEntity<int> {
        public int CompanyId { get; set; }
        public string? CardUserKey { get; set; }
        public string? CardToken { get; set; }
        public string? CardAlias { get; set; }
        public string? BinNumber { get; set; }
        public string? CardAssociation { get; set; }
        public string? CardFamily { get; set; }
        public string? CardBankCode { get; set; }
        public string? CardBankName { get; set; }
        public string? CartType { get; set; }
        public string? CardHolderName { get; set; }
        public int ExpireMonth { get; set; }
        public int ExpireYear { get; set; }

        [ForeignKey("CompanyId")]
        public Company Company { get; set; }
    }
}

