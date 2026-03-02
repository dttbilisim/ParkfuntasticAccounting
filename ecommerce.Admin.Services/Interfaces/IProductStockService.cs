using ecommerce.Admin.Domain.Dtos.ProductStockDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IProductStockService
    {
        Task<IActionResult<Paging<List<ProductStockListDto>>>> GetStocks(PageSetting pager);
        Task<IActionResult<List<ProductStockListDto>>> GetStocksByProduct(int productId);
        Task<IActionResult<List<ProductStockListDto>>> GetStocksByShelf(int shelfId);
        Task<IActionResult<Empty>> UpsertStock(AuditWrapDto<ProductStockUpsertDto> model);
        Task<IActionResult<Empty>> DeleteStock(AuditWrapDto<ProductStockDeleteDto> model);
        Task<IActionResult<ProductStockUpsertDto>> GetStockById(int Id);
        Task<IActionResult<Empty>> TransferStock(AuditWrapDto<ProductStockTransferDto> model);
        Task<IActionResult<Paging<List<StockTransferLogListDto>>>> GetTransferLogs(PageSetting pager);
        Task<IActionResult<Paging<List<StockTransferBatchDto>>>> GetTransferBatches(PageSetting pager);
        Task<IActionResult<List<StockTransferLogListDto>>> GetTransferLogsByBatchId(Guid batchId);
        Task<IActionResult<Empty>> UpdateTransferLogQuantity(int logId, decimal newQuantity, int userId);
    }
}
