namespace ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto
{
    /// <summary>
    /// Kasa yürüyen bakiye özeti — açılış, giriş, çıkış, güncel bakiye (kasanın kendi dövizi)
    /// </summary>
    public class CashRegisterBalanceSummaryDto
    {
        public int CashRegisterId { get; set; }
        public string CashRegisterName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        /// <summary>Açılış + Toplam Giriş - Toplam Çıkış</summary>
        public decimal CurrentBalance => OpeningBalance + TotalIn - TotalOut;
    }
}
