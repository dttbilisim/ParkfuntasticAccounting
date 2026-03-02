using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto
{
    public class CashRegisterMovementUpsertDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Kasa seçimi zorunludur")]
        public int CashRegisterId { get; set; }

        [Required(ErrorMessage = "Hareket tipi zorunludur")]
        public CashRegisterMovementType MovementType { get; set; }

        public int? CustomerId { get; set; }

        public int? PaymentTypeId { get; set; }

        [Required(ErrorMessage = "Döviz seçimi zorunludur")]
        public int CurrencyId { get; set; }

        [Required(ErrorMessage = "Tutar zorunludur")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Tarih zorunludur")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
