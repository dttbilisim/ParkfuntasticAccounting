namespace ecommerce.Admin.Domain.Dtos.DashboardDto;

public class DashboardDto
{
    public DashboardOrderSummaryDto OrderSummary { get; set; } = new();
    public int TotalUsers { get; set; }
    public List<DashboardBestSellingProductDto> BestSellingProducts { get; set; } = new();
    public List<DashboardSellerSalesDto> TopSellers { get; set; } = new();
}

public class DashboardOrderSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal NewOrdersRevenue { get; set; }
    public decimal CompletedOrdersRevenue { get; set; }
    public decimal CancelledOrdersRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrdersCount { get; set; }
}

public class DashboardBestSellingProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class DashboardSellerSalesDto
{
    public int SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public decimal TotalSalesAmount { get; set; }
    public int OrderCount { get; set; }
}

public class DashboardChartDto
{
    public string Date { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int OrderCount { get; set; }
}
