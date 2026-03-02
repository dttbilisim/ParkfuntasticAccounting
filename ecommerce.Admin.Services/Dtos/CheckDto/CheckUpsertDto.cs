using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CheckDto
{
    public class CheckUpsertDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Cari seçimi zorunludur")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Banka seçimi zorunludur")]
        public int BankId { get; set; }

        public int? BankBranchId { get; set; }

        /// <summary>Boş bırakılırsa sunucuda otomatik üretilir (CHK-yyyyMMdd-0001).</summary>
        [MaxLength(50)]
        public string? CheckNumber { get; set; }

        [Required(ErrorMessage = "Tutar zorunludur")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vade tarihi zorunludur")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Döviz seçimi zorunludur")]
        public int CurrencyId { get; set; }

        public CheckStatus CheckStatus { get; set; } = CheckStatus.InPortfolio;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime? ReceivedDate { get; set; }
        public DateTime? SettlementDate { get; set; }

        /// <summary>Sunucuda set edilir; DTO'dan atanmaz. Tenant yapısı için BranchId _tenantProvider'dan alınır.</summary>
        public int? BranchId { get; set; }
    }
}
