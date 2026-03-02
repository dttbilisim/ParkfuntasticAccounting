using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using Microsoft.Extensions.Caching.Memory;
using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Dtos.InvoiceDto;
using Microsoft.EntityFrameworkCore;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Identity;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Admin.Services.Concreate;

public class DashboardCacheService : IDashboardCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IOrderService _orderService;
    private readonly IInvoiceService _invoiceService;
    private readonly ICustomerAccountTransactionService _accountService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ISalesPersonService _salesPersonService;
    private readonly CurrentUser _currentUser;

    private const string CACHE_KEY_PREFIX = "dashboard_data_";
    private static readonly TimeSpan CACHE_EXPIRATION = TimeSpan.FromMinutes(2); // 5 dakika cache (optimized from 30s)
    private static readonly TimeSpan BACKGROUND_REFRESH_THRESHOLD = TimeSpan.FromMinutes(4); // 4 dakika sonra arka planda refresh

    public DashboardCacheService(
        IMemoryCache cache, 
        IOrderService orderService, 
        IInvoiceService invoiceService, 
        ICustomerAccountTransactionService accountService,
        IServiceScopeFactory scopeFactory,
        ITenantProvider tenantProvider,
        ISalesPersonService salesPersonService,
        CurrentUser currentUser)
    {
        _cache = cache;
        _orderService = orderService;
        _invoiceService = invoiceService;
        _accountService = accountService;
        _scopeFactory = scopeFactory;
        _tenantProvider = tenantProvider;
        _salesPersonService = salesPersonService;
        _currentUser = currentUser;
    }

    public async Task<DashboardDataDto?> GetDashboardDataAsync(int userId, int? customerId = null)
    {
        var cacheKey = GetCacheKey(userId, customerId);
        
        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out DashboardDataDto? cachedData) && cachedData != null)
        {
            return cachedData;
        }

        // Cache miss - load and cache
        return await RefreshDashboardDataAsync(userId, customerId);
    }

    public async Task<DashboardDataDto?> RefreshDashboardDataAsync(int userId, int? customerId = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Capture the current principal to seed the new scope
        var principal = _currentUser.Principal;

        // Use IServiceScopeFactory to create an independent scope.
        // This prevents ObjectDisposedException and concurrency issues 
        // when both Footer and Dashboard hit the service at the same time.
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        try
        {
            // CRITICAL: Seed the new scope with the current user context
            if (principal != null)
            {
                var scopedCurrentUser = sp.GetRequiredService<CurrentUser>();
                scopedCurrentUser.SetUser(principal);
            }

            var scopedTenantProvider = sp.GetRequiredService<ITenantProvider>();
            var scopedOrderService = sp.GetRequiredService<IOrderService>();
            var scopedInvoiceService = sp.GetRequiredService<IInvoiceService>();
            var scopedAccountService = sp.GetRequiredService<ICustomerAccountTransactionService>();
            var scopedSalesPersonService = sp.GetRequiredService<ISalesPersonService>();

            // If customerId is null but user IS a B2B Customer, we MUST resolve their customer ID
            if (!customerId.HasValue && scopedTenantProvider.IsCustomerB2B)
            {
                customerId = scopedTenantProvider.GetCustomerId();
            }

            List<OrderListDto> orders = new();
            List<InvoiceListDto> invoices = new();
            decimal balance = 0m;
            decimal totalDebit = 0m;
            decimal totalCredit = 0m;

            if (customerId.HasValue)
            {
                // Customer-specific queries
                var orderResult = await scopedOrderService.GetCustomerOrders(customerId.Value);
                orders = orderResult.Ok && orderResult.Result != null ? orderResult.Result : new List<OrderListDto>();

                try
                {
                    var invoiceResult = await scopedInvoiceService.GetCustomerInvoices(customerId.Value);
                    invoices = invoiceResult.Ok && invoiceResult.Result != null ? invoiceResult.Result : new List<InvoiceListDto>();
                }
                catch { }

                try
                {
                    var accountResult = await scopedAccountService.GetCustomerAccountReport(customerId.Value, null, null);
                    if (accountResult.Ok && accountResult.Result != null)
                    {
                        var report = accountResult.Result;
                        balance = report.Balance;
                        totalDebit = report.TotalDebit;
                        totalCredit = report.TotalCredit;
                    }
                }
                catch { }
            }
            else
            {
                // Plasiyer aggregated queries
                var plasiyerResult = await scopedOrderService.GetPlasiyerCustomersOrders(userId);
                if (plasiyerResult.Ok && plasiyerResult.Result != null)
                {
                    orders.AddRange(plasiyerResult.Result);
                }

                var myOrdersResult = await scopedOrderService.GetMyOrders(userId);
                if (myOrdersResult.Ok && myOrdersResult.Result != null)
                {
                    foreach (var order in myOrdersResult.Result)
                    {
                        if (!orders.Any(o => o.Id == order.Id))
                        {
                            orders.Add(order);
                        }
                    }
                }

                try
                {
                    var plasiyerInvResult = await scopedInvoiceService.GetPlasiyerCustomersInvoices(userId);
                    invoices = plasiyerInvResult.Ok && plasiyerInvResult.Result != null 
                        ? plasiyerInvResult.Result 
                        : new List<InvoiceListDto>();
                }
                catch { }

                try
                {
                    var accountRes = await scopedAccountService.GetPlasiyerAccountSummary(userId);
                    if (accountRes.Ok && accountRes.Result != null)
                    {
                        balance = accountRes.Result.Balance;
                        totalDebit = accountRes.Result.TotalDebit;
                        totalCredit = accountRes.Result.TotalCredit;
                    }
                }
                catch { }
            }
            
            // Calculate aggregated metrics
            var pendingOrders = orders
                .Where(o => o.OrderStatusType == OrderStatusType.OrderNew || 
                           o.OrderStatusType == OrderStatusType.OrderPrepare ||
                           o.OrderStatusType == OrderStatusType.OrderWaitingApproval ||
                           o.OrderStatusType == OrderStatusType.OrderWaitingPayment ||
                           o.OrderStatusType == OrderStatusType.OrderinCargo) 
                .ToList();
            
            var dashboardData = new DashboardDataDto
            {
                PendingOrderCount = pendingOrders.Count,
                PendingOrderTotal = pendingOrders.Sum(o => o.GrandTotal),
                TotalOrderCount = orders.Count,
                TotalOrderAmount = orders.Sum(o => o.GrandTotal),
                Balance = balance,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                TotalInvoiceCount = invoices.Count,
                TotalInvoiceAmount = invoices.Sum(i => i.GeneralTotal),
                LinkedCustomerCount = 0,
                CachedAt = DateTime.UtcNow
            };

            // Count linked customers for Plasiyer
            if (!customerId.HasValue) 
            {
                try 
                {
                    var salesPersonId = scopedTenantProvider.GetSalesPersonId();
                    if (salesPersonId.HasValue)
                    {
                        var customersResult = await scopedSalesPersonService.GetCustomersOfSalesPerson(salesPersonId.Value);
                        if (customersResult.Ok && customersResult.Result != null)
                        {
                            dashboardData.LinkedCustomerCount = customersResult.Result.Count;
                        }
                    }
                }
                catch {}
            }

            _cache.Set(GetCacheKey(userId, customerId), dashboardData, CACHE_EXPIRATION);
            
            sw.Stop();
            _cache.Set($"last_refresh_duration_{userId}", sw.ElapsedMilliseconds);

            return dashboardData;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public void InvalidateCache(int userId)
    {
        // Invalidate both personal view and aggregated view
        _cache.Remove(GetCacheKey(userId, null));
        
        // We don't know exactly which customer was selected, 
        // but it's okay because GetCacheKey handles it. 
        // Optimization: the UI caller should ideally pass the customerId if it knows it.
    }

    public void InvalidateCache(int userId, int? customerId)
    {
        _cache.Remove(GetCacheKey(userId, customerId));
    }

    public void InvalidateAllCache()
    {
        // Note: IMemoryCache doesn't support pattern-based removal
        // In production, consider using distributed cache (Redis) with pattern matching
        // For now, we'll rely on expiration
    }

    private static string GetCacheKey(int userId, int? customerId)
    {
        return customerId.HasValue 
            ? $"{CACHE_KEY_PREFIX}{userId}_C{customerId.Value}" 
            : $"{CACHE_KEY_PREFIX}{userId}_Total";
    }
}
