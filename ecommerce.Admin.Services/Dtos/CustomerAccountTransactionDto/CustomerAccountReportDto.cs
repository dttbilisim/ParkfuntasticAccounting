namespace ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto
{
    public class CustomerAccountReportDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        /// <summary>Makbuz e-postası için cari e-posta adresi (mobilde input dolu gelsin diye)</summary>
        public string? CustomerEmail { get; set; }
        public decimal TotalDebit { get; set; } // Toplam Borç
        public decimal TotalCredit { get; set; } // Toplam Alacak
        public decimal Balance { get; set; } // Bakiye (Borç - Alacak)
        public List<CustomerAccountTransactionListDto> Transactions { get; set; } = new();
    }
}
