using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Services.Concreate;

public class OnlineUserService : IOnlineUserService
{
    private readonly IRedisCacheService _redisCacheService;
    private readonly ApplicationDbContext _dbContext;

    public OnlineUserService(IRedisCacheService redisCacheService, ApplicationDbContext dbContext)
    {
        _redisCacheService = redisCacheService;
        _dbContext = dbContext;
    }

    public async Task<List<OnlineUserDto>> GetOnlineUsersAsync(int timeWindowMinutes = 60)
    {
        var nowSeconds = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        var minScore = nowSeconds - (timeWindowMinutes * 60);

        // Get UserIds active in window
        var userIds = await _redisCacheService.GetRangeFromSortedSetByScoreAsync("online_users", minScore, double.PositiveInfinity);
        
        var users = new List<OnlineUserDto>();
        foreach (var userId in userIds)
        {
            var user = await _redisCacheService.GetAsync<OnlineUserDto>($"online_user_detail:{userId}");
            if (user != null)
            {
                users.Add(user);
            }
        }
        
        return users;
    }

    /// <summary>
    /// BranchId bazlı filtreleme ile konum bilgisi olan online plasiyerleri getirir.
    /// Redis'ten online kullanıcıları çeker, DB'den UserBranch tablosu ile BranchId filtreler.
    /// </summary>
    public async Task<List<OnlineUserDto>> GetOnlineSalesmenWithLocationAsync(int branchId, int timeWindowMinutes = 65)
    {
        // 1. Redis'ten tüm online kullanıcıları çek
        var allOnlineUsers = await GetOnlineUsersAsync(timeWindowMinutes);
        
        // 2. Sadece konum bilgisi olanları filtrele (Latitude ve Longitude dolu olanlar)
        var usersWithLocation = allOnlineUsers
            .Where(u => u.Latitude.HasValue && u.Longitude.HasValue)
            .ToList();

        if (!usersWithLocation.Any())
            return new List<OnlineUserDto>();

        // 3. Bu kullanıcıların UserId'lerini al
        var onlineUserIds = usersWithLocation
            .Select(u => int.TryParse(u.UserId, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        // 4. DB'den UserBranch tablosu ile BranchId filtrelemesi yap
        // Aynı zamanda Plasiyer rolünde olduklarını doğrula
        var salesmenInBranch = await _dbContext.Set<ecommerce.Core.Entities.Hierarchical.UserBranch>()
            .AsNoTracking()
            .Where(ub => ub.BranchId == branchId && onlineUserIds.Contains(ub.UserId))
            .Select(ub => ub.UserId)
            .ToListAsync();

        // Plasiyer rolünde olanları filtrele
        var plasiyerUserIds = await _dbContext.Set<ecommerce.Core.Entities.Authentication.ApplicationUser>()
            .AsNoTracking()
            .Where(u => salesmenInBranch.Contains(u.Id) && u.SalesPersonId != null)
            .Select(u => u.Id.ToString())
            .ToListAsync();

        // 5. Sonuçları birleştir
        return usersWithLocation
            .Where(u => plasiyerUserIds.Contains(u.UserId))
            .ToList();
    }

    /// <summary>
    /// Belirli bir plasiyerin günlük konum geçmişini Redis'ten getirir
    /// Key formatı: location_history:{userId}:{yyyy-MM-dd}
    /// </summary>
    public async Task<List<LocationHistoryPointDto>> GetLocationHistoryAsync(string userId, string date)
    {
        var historyKey = $"location_history:{userId}:{date}";
        var entries = await _redisCacheService.ListRangeAsync(historyKey);

        var points = new List<LocationHistoryPointDto>();
        foreach (var entry in entries)
        {
            try
            {
                var point = System.Text.Json.JsonSerializer.Deserialize<LocationHistoryPointDto>(entry,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (point != null)
                    points.Add(point);
            }
            catch
            {
                // Bozuk veri — atla
            }
        }

        return points.OrderBy(p => p.Ts).ToList();
    }
}
