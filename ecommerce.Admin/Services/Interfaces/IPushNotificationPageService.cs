using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>
/// Push bildirim sayfası için servis interface'i.
/// Bildirim geçmişi sorgulama ve kullanıcı arama işlemlerini sağlar.
/// </summary>
public interface IPushNotificationPageService
{
    /// <summary>Bildirim geçmişini sayfalı olarak getirir</summary>
    Task<(List<NotificationLog> Items, int TotalCount)> GetNotificationLogsAsync(int skip, int take);

    /// <summary>Kullanıcı arama — ad, soyad veya e-posta ile filtreleme</summary>
    Task<List<ApplicationUser>> SearchUsersAsync(string? searchTerm, int take = 50);

    /// <summary>Ürün arama — ad veya barkod ile filtreleme</summary>
    Task<List<DeepLinkItem>> SearchProductsAsync(string? searchTerm, int take = 30);

    /// <summary>Kategori arama — ad ile filtreleme</summary>
    Task<List<DeepLinkItem>> SearchCategoriesAsync(string? searchTerm, int take = 30);

    /// <summary>Marka arama — ad ile filtreleme</summary>
    Task<List<DeepLinkItem>> SearchBrandsAsync(string? searchTerm, int take = 30);

    /// <summary>Aktif kampanyaları getir — ad ile filtreleme</summary>
    Task<List<DeepLinkCampaignItem>> SearchCampaignsAsync(string? searchTerm, int take = 30);
}

/// <summary>Deep link seçici için basit DTO</summary>
public record DeepLinkItem(int Id, string Name);

/// <summary>Kampanya deep link DTO'su — tip ve atanmış entity ID'leri ile</summary>
public record DeepLinkCampaignItem(int Id, string Name, int DiscountType, List<int>? AssignedEntityIds);
