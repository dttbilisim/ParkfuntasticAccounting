using ecommerce.Admin.Services;
using ecommerce.Admin.Domain.Dtos.DashboardDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages {
    public partial class Index
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Inject]
        protected IOrderService OrderService { get; set; }

        [Inject]
        protected IUserService UserService { get; set; }

        [Inject]
        protected ITenantProvider TenantProvider { get; set; }

        protected DashboardOrderSummaryDto OrderSummary = new();
        protected int TotalUsers = 0;
        protected List<DashboardBestSellingProductDto> BestSellingProducts = new();
        protected List<DashboardSellerSalesDto> TopSellers = new();
        protected List<DashboardChartDto> OrderStatsOverTime = new();
        protected List<KeyValuePair<string, int>> Metrics = new();

        protected override async Task OnInitializedAsync()
        {
            if (Security.User != null && (
                Security.User.CustomerId.HasValue || 
                Security.User.Roles.Any(r => r.Name == "Plasiyer" || r.Name == "CustomerB2B")))
            {
                NavigationManager.NavigateTo("product-search");
                return;
            }

            if (Security.User == null)
            {
                return;
            }

            // Parallelize all dashboard data fetching for maximum performance
            var orderSummaryTask = OrderService.GetDashboardOrderSummary();
            var userCountTask = UserService.GetUserCount();
            var bestSellingTask = OrderService.GetBestSellingProducts(10);
            var topSellersTask = OrderService.GetSalesBySeller(10);
            var chartTask = OrderService.GetOrderStatsOverTime(30);
            var statusMetricsTask = OrderService.OrderStatus();

            await Task.WhenAll(orderSummaryTask, userCountTask, bestSellingTask, topSellersTask, chartTask, statusMetricsTask);

            // Assign results
            var orderSummaryResult = await orderSummaryTask;
            if (orderSummaryResult.Ok)
            {
                OrderSummary = orderSummaryResult.Result;
            }

            var userCountResult = await userCountTask;
            if (userCountResult.Ok)
            {
                TotalUsers = userCountResult.Result;
            }

            var bestSellingResult = await bestSellingTask;
            if (bestSellingResult.Ok)
            {
                BestSellingProducts = bestSellingResult.Result;
            }

            var topSellersResult = await topSellersTask;
            if (topSellersResult.Ok)
            {
                TopSellers = topSellersResult.Result;
            }

            var chartResult = await chartTask;
            if (chartResult.Ok)
            {
                OrderStatsOverTime = chartResult.Result;
            }

            Metrics = await statusMetricsTask;
        }
    }
}