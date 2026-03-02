using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto
{
    /// <summary>
    /// Kasalar arası virman — kaynak kasadan çıkış, hedef kasaya giriş (muhasebe mantığında iki hareket).
    /// </summary>
    public class CashRegisterTransferDto
    {
        [Required(ErrorMessage = "Kaynak kasa seçimi zorunludur")]
        public int SourceCashRegisterId { get; set; }

        [Required(ErrorMessage = "Hedef kasa seçimi zorunludur")]
        public int TargetCashRegisterId { get; set; }

        [Required(ErrorMessage = "Tutar zorunludur")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Döviz zorunludur")]
        public int CurrencyId { get; set; }

        [Required(ErrorMessage = "Tarih zorunludur")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
