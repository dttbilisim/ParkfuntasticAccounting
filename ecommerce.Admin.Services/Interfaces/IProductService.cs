using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IProductService
    {
        public Task<IActionResult<Paging<List<ProductListDto>>>> GetProducts(PageSetting pager);
        public Task<IActionResult<List<ProductListDto>>> SearchProducts(string search);
        public Task<IActionResult<List<ProductListDto>>> GetProducts();
      
        public Task<IActionResult<List<ProductListDto>>> GetProducts(List<int> Ids);
        Task<IActionResult<int>> UpsertProduct(AuditWrapDto<ProductUpsertDto> model);
        Task<IActionResult<Empty>> DeleteProduct(AuditWrapDto<ProductDeleteDto> model);
        Task<IActionResult<ProductUpsertDto>> GetProductById(int productId);
        Task<IActionResult<ProductUpsertDto>> GetProductByBarcode(string barcode);
        
        Task<IActionResult<string>> UpsertProductImport(ProductTransaction model);
        public Task<IActionResult<Paging<List<ProductTransactionDto>>>> GetProductsImportList(PageSetting pager);
        
        Task<IActionResult<bool>> UpsertProductLastCheckImport();

        Task<List<DuplicateProductListDto>> GetDublicateProductList();
        Task<IActionResult<bool>> GetDuplicateProductDelete(DuplicateProductDeleteDto model);
        
        Task<IActionResult<List<ProductAdvertListDto>>> GetProductAdvertListById(int productId);
        Task<IActionResult<List<ProductSellerItemListDto>>> GetSellerItemsByProduct(int productId);
        Task<IActionResult<Paging<List<ProductOnlineDto>>>> GetProductOnline(PageSetting pager);
        Task<IActionResult<List<ProductDublicaListDto>>> GetDublicateProductListWitGroup();
        Task<IActionResult<bool>> MergeProductsAsync(MergeProductUpsertDto model);
        Task<IActionResult<List<ProductCompatibleVehicleDto>>> GetCompatibleVehicles(int productId);

    }
}
