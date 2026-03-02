using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Web.Domain.Services.Concreate;

public class DotVehicleDataService : IDotVehicleDataService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    public DotVehicleDataService(IUnitOfWork<ApplicationDbContext> context)
    {
        _context = context;
    }

    public async Task<IActionResult<List<DotVehicleData>>> GetVehiclesByPartsAsync(List<int> vehicleTypes, List<int> baseModelKeys)
    {
        var rs = OperationResult.CreateResult<List<DotVehicleData>>();
        try
        {
            if (!vehicleTypes.Any() || !baseModelKeys.Any())
            {
                rs.Result = new List<DotVehicleData>();
                return rs;
            }

            var vehicles = await _context.DbContext.Set<DotVehicleData>()
                .AsNoTracking()
                .Where(v => vehicleTypes.Contains(v.VehicleType ?? 0) && 
                           baseModelKeys.Contains(v.BaseModel ?? 0) && 
                           v.IsActive)
                .OrderBy(v => v.ManufacturerName)
                .ThenBy(v => v.BaseModelName)
                .ThenBy(v => v.SubModelName)
                .ToListAsync();

            rs.Result = vehicles;
        }
        catch (Exception ex)
        {
            rs.AddSystemError($"Araç verileri alınırken hata oluştu: {ex.Message}");
        }
        
        return rs;
    }

    public async Task<IActionResult<DotVehicleData?>> GetVehicleByDatECodeAsync(string datECode)
    {
        var rs = OperationResult.CreateResult<DotVehicleData?>();
        try
        {
            var vehicle = await _context.DbContext.Set<DotVehicleData>()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.DatECode == datECode && v.IsActive);

            rs.Result = vehicle;
        }
        catch (Exception ex)
        {
            rs.AddSystemError($"Araç verisi alınırken hata oluştu: {ex.Message}");
        }
        
        return rs;
    }
}

