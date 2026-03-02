using BasbugOto.Dtos;
namespace BasbugOto.Abstract;
public interface IBasbugApiService
{
    Task<string?> GetTokenAsync();
    Task<List<BasbugGroupDto>> GetGroupsAsync();
    Task<List<BasbugProductDto>> GetProductsByGroupAsync(string grupKodu, string depo);
    Task<List<BasbugStockDto>> GetStockByGroupAsync(string grupKodu, string depo);
    
}