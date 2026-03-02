using ecommerce.Admin.Domain.Dtos.SellerAddressDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ISellerAddressService
    {
        Task<IActionResult<List<SellerAddressListDto>>> GetSellerAddresses(int sellerId);
        Task<IActionResult<SellerAddressUpsertDto>> GetSellerAddressById(int id);
        Task<IActionResult<int>> UpsertSellerAddress(AuditWrapDto<SellerAddressUpsertDto> model);
        Task<IActionResult<Empty>> DeleteSellerAddress(AuditWrapDto<SellerAddressDeleteDto> model);
    }
}
