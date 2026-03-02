using ecommerce.Admin.Domain.Dtos.CargoDto;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ICargoCreationService
    {
        /// <summary>
        /// Creates cargo shipment for products in a specific warehouse
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <param name="warehouseName">Warehouse name (e.g., "ANKARA", "PLAZA")</param>
        /// <param name="productIds">List of product IDs in this warehouse</param>
        /// <returns>Result of cargo creation operation</returns>
        Task<CargoCreationResultDto> CreateCargoForWarehouseGroupAsync(int orderId, string warehouseName, List<int> productIds);

        /// <summary>
        /// Cancels Sendeo cargo shipment for products in a specific warehouse
        /// </summary>
        Task<CargoCreationResultDto> CancelSendeoCargoAsync(int orderId, string warehouseName, List<int> productIds);
    }
}
