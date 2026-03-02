using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto
{
    public class CashRegisterMovementListDto
    {
        public int Id { get; set; }
        public int CashRegisterId { get; set; }
        public string CashRegisterName { get; set; } = string.Empty;
        public CashRegisterMovementType MovementType { get; set; }
        public string MovementTypeName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? PaymentTypeId { get; set; }
        public string? PaymentTypeName { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
    }
}
