using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto
{
    public class CustomerAccountTransactionListDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceNo { get; set; }
        /// <summary>
        /// e-Fatura ETTN numarası — mobil uygulamada fatura indirme için kullanılır
        /// </summary>
        public string? Ettn { get; set; }
        public CustomerAccountTransactionType TransactionType { get; set; }
        public string TransactionTypeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public PaymentType? PaymentTypeId { get; set; }
        public string? PaymentTypeName { get; set; }
        public int? CashRegisterId { get; set; }
        public string? CashRegisterName { get; set; }
        public string? ReferenceNo { get; set; }
        public decimal BalanceAfterTransaction { get; set; }
        
        // Yürüyen bakiye için
        public decimal IncomingAmount { get; set; } // Giren (Alacak)
        public decimal OutgoingAmount { get; set; } // Çıkan (Borç)
        
        /// <summary>
        /// Faturaya bağlı tüm siparişler (birden fazla sipariş aynı faturaya bağlanabilir)
        /// </summary>
        public List<LinkedOrderDto> LinkedOrders { get; set; } = new();
    }
    
    /// <summary>
    /// Cari hesap hareketine bağlı sipariş bilgisi
    /// </summary>
    public class LinkedOrderDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
    }
}
