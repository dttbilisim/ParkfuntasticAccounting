using AutoMapper;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CashRegisterDto
{
    [AutoMap(typeof(CashRegister))]
    public class CashRegisterListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public decimal OpeningBalance { get; set; }
        public bool IsDefault { get; set; }
        public int? PaymentTypeId { get; set; }
        public string? PaymentTypeName { get; set; }
        public DateTime CreatedDate { get; set; }
        public EntityStatus Status { get; set; }
    }
}
