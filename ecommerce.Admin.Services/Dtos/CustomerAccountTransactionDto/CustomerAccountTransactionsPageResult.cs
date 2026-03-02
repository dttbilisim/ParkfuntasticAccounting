namespace ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto
{
    /// <summary>
    /// Cari hareketler sayfası sonucu: veri + filtre carileri + cari bazlı alt toplamlar (backend'den gelir).
    /// </summary>
    public class CustomerAccountTransactionsPageResult
    {
        public List<CustomerAccountTransactionListDto> Data { get; set; } = new();
        public int DataCount { get; set; }
        public int TotalRawCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        /// <summary>Filtre dropdown için: hareketi olan cariler (aynı tarih/müşteri filtresine göre).</summary>
        public List<FilterCustomerItemDto> FilterCustomers { get; set; } = new();

        /// <summary>Cari bazlı alt toplamlar (bu sayfa verisi üzerinden backend'de hesaplanır).</summary>
        public List<CustomerSubtotalItemDto> CustomerSubtotals { get; set; } = new();
    }

    /// <summary>Filtre dropdown için cari öğesi (Id + görünen ad).</summary>
    public class FilterCustomerItemDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>Cari bazlı alt toplam satırı (Borç, Alacak, Net).</summary>
    public class CustomerSubtotalItemDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Net { get; set; }
    }
}
