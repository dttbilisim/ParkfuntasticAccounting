using ecommerce.Domain.Shared.Dtos.OtoIsmail;

namespace ecommerce.Domain.Shared.Abstract;

public interface IOtoIsmailService
{
    Task<string?> LoginAsync(string username, string password);
    Task<OtoIsmailResultBrandsDto?> GetBrandsAsync();
    Task<OtoIsmailResultProductsDto?> GetProductsAsync(string marka, string tarih = "19000101");
    Task<OtoIsmailResultStockByCodeDto?> GetStockByCodeAsync(string productCode);
    Task<OtoIsmailResultStockIdDto?> GetStockByIdAsync(int stokId);
    Task<OtoIsmailResultStockByDateDto?> GetStockByDateAsync(string tarih = "19000101");
    Task<OtoIsmailResultCurrencyDto?> GetCurrencyAsync();
    Task<OISMLServiceResultDto?> AddToCartAsync(List<OtoIsmailCartItemDto> items);
    Task<OISMLServiceResultDto?> SendOrderAsync(string teslimatCariKodu, string aciklama, bool bizAlacagiz, List<OtoIsmailCartItemDto> items);
    Task<OtoIsmailResultProductsDto?> GetProductsUpdateAsync(int hour);
    Task<OISMLResultDto?> CheckStockAsync(string stokKodu, int? netsisStokId, int adet);
    Task<OtoIsmailResultCategoryDto?> GetCategoriesAsync();
    Task<OtoIsmailResultCategoryProductDto?> GetCategoryProductsAsync(string k1, string k2, string k3, string k4);
    Task<OtoIsmailResultOrderStatusDto?> GetOrderStatusAsync(string siparisId);
    Task<OtoIsmailResultVehicleBrandDto?> GetVehicleBrandsAsync();
    Task<OtoIsmailResultVehicleProductDto?> GetVehicleBrandProductsAsync(string aracMarka);
    Task<OISMLServiceResultDto?> CancelOrderAsync(string siparisId);
    Task<OtoIsmailResultStockCodeChangeDto?> GetTodayChangedStockCodesAsync();
    Task<OtoIsmailResultCariListesiDto?> GetCariListesiAsync();
    Task<OtoIsmailResultIlIlceListesiDto?> GetIlIlceListesiAsync();
}
