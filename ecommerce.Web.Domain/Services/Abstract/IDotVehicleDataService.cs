using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IDotVehicleDataService
{
    Task<IActionResult<List<DotVehicleData>>> GetVehiclesByPartsAsync(List<int> vehicleTypes, List<int> baseModelKeys);
    Task<IActionResult<DotVehicleData?>> GetVehicleByDatECodeAsync(string datECode);
}

