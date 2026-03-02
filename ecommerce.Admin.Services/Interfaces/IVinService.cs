using ecommerce.Core.Utils.ResultSet;
using ecommerce.Admin.Services.Dtos.VinDto;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IVinService
    {
        Task<IActionResult<VinDecodeResultDto>> DecodeVinAsync(string vinNumber);
        Task<IActionResult<List<string>>> GetOemCodesByManufacturerAsync(string manufacturerName);
    }
}
