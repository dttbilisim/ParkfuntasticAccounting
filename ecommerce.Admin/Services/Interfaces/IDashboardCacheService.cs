namespace ecommerce.Admin.Services.Interfaces;

public interface IDashboardCacheService
{
    Task<DashboardDataDto?> GetDashboardDataAsync(int userId, int? customerId = null);
    Task<DashboardDataDto?> RefreshDashboardDataAsync(int userId, int? customerId = null);
    void InvalidateCache(int userId);
    void InvalidateCache(int userId, int? customerId);
    void InvalidateAllCache();
}

public class DashboardDataDto
{
    public int PendingOrderCount { get; set; }
    public decimal PendingOrderTotal { get; set; }
    public int TotalOrderCount { get; set; }
    public decimal TotalOrderAmount { get; set; }
    public decimal Balance { get; set; }
    public int TotalInvoiceCount { get; set; }
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public int LinkedCustomerCount { get; set; }
    public DateTime CachedAt { get; set; }
}
