using System.Collections.Generic;
using System.Threading.Tasks;
using ecommerce.Domain.Shared.Dtos;

namespace ecommerce.Admin.Services.Interfaces;

public interface IOnlineUserService
{
    Task<List<OnlineUserDto>> GetOnlineUsersAsync(int timeWindowMinutes = 5);
    
    /// <summary>
    /// BranchId bazlı filtreleme ile konum bilgisi olan online plasiyerleri getirir
    /// </summary>
    Task<List<OnlineUserDto>> GetOnlineSalesmenWithLocationAsync(int branchId, int timeWindowMinutes = 65);

    /// <summary>
    /// Belirli bir plasiyerin günlük konum geçmişini Redis'ten getirir
    /// </summary>
    Task<List<LocationHistoryPointDto>> GetLocationHistoryAsync(string userId, string date);
}
