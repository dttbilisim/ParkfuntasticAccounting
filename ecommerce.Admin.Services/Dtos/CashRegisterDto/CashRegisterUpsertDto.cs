using AutoMapper;
using ecommerce.Core.Entities.Accounting;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.CashRegisterDto
{
    [AutoMap(typeof(CashRegister), ReverseMap = true)]
    public class CashRegisterUpsertDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Kasa adı zorunludur")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Döviz seçimi zorunludur")]
        public int CurrencyId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal OpeningBalance { get; set; }

        public bool IsDefault { get; set; }
        public int? PaymentTypeId { get; set; }
        public int CorporationId { get; set; }
        public int BranchId { get; set; }
    }
}
