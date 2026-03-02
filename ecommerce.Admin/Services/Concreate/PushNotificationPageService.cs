using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Services.Concreate;

/// <summary>
/// Push bildirim sayfası servis implementasyonu.
/// IServiceScopeFactory kullanarak her sorgu için bağımsız DbContext oluşturur —
/// Blazor Server'da eşzamanlı sorgu çakışmalarını (NpgsqlOperationInProgressException) önler.
/// DashboardCacheService ile aynı pattern'i takip eder.
/// </summary>
public class PushNotificationPageService : IPushNotificationPageService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PushNotificationPageService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<(List<NotificationLog> Items, int TotalCount)> GetNotificationLogsAsync(int skip, int take)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var query = db.NotificationLogs.AsNoTracking();
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<List<ApplicationUser>> SearchUsersAsync(string? searchTerm, int take = 50)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var query = db.AspNetUsers.AsNoTracking();

        // Filtre varsa uygula
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(u => u.FirstName)
            .Take(take)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<DeepLinkItem>> SearchProductsAsync(string? searchTerm, int take = 30)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var query = db.Product.AsNoTracking().Where(p => p.Status == 1);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new DeepLinkItem(p.Id, p.Name))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<DeepLinkItem>> SearchCategoriesAsync(string? searchTerm, int take = 30)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var query = db.Category.AsNoTracking().Where(c => c.Status == 1);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new DeepLinkItem(c.Id, c.Name))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<DeepLinkItem>> SearchBrandsAsync(string? searchTerm, int take = 30)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var query = db.Brand.AsNoTracking().Where(b => b.Status == 1);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(b => b.Name)
            .Take(take)
            .Select(b => new DeepLinkItem(b.Id, b.Name))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<DeepLinkCampaignItem>> SearchCampaignsAsync(string? searchTerm, int take = 30)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var query = db.Discounts.AsNoTracking()
            .Where(d => d.Status == 1)
            .Where(d => d.StartDate == null || d.StartDate <= now)
            .Where(d => d.EndDate == null || d.EndDate >= now);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(d => d.Name)
            .Take(take)
            .Select(d => new DeepLinkCampaignItem(
                d.Id, d.Name, (int)d.DiscountType, d.AssignedEntityIds))
            .ToListAsync();
    }
}
