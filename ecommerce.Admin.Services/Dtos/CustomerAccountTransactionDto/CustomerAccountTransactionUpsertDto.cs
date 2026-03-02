using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto
{
    public class CustomerAccountTransactionUpsertDto
    {
        public int? Id { get; set; }
        public int CustomerId { get; set; }
        public int? OrderId { get; set; }
        public int? InvoiceId { get; set; }
        public CustomerAccountTransactionType TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string? Description { get; set; }
        public PaymentType? PaymentTypeId { get; set; }
        public int? CashRegisterId { get; set; }
        public string? ReferenceNo { get; set; }
    }
}
