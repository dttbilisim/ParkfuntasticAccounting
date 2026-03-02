using Otokoc.Dto;
namespace Otokoc.Abstract;
public interface IOtokocService
{
    Task<List<Product>> GetAllProductsByPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}