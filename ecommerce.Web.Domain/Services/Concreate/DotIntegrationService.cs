using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
namespace ecommerce.Web.Domain.Services.Concreate;

public class DotIntegrationService : IDotIntegrationService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ILogger<DotIntegrationService> _logger;

    public DotIntegrationService(IUnitOfWork<ApplicationDbContext> context, ILogger<DotIntegrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region 1. Araç Türleri
    public async Task<IActionResult<List<DotVehicleType>>> GetVehicleTypesAsync()
    {
        var rs = OperationResult.CreateResult<List<DotVehicleType>>();
        try
        {
            _logger.LogInformation("Araç türleri getiriliyor...");
            
            var vehicleTypes = await _context.DbContext.Set<DotVehicleType>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = vehicleTypes;
            _logger.LogInformation($"{vehicleTypes.Count} araç türü getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç türleri getirilirken hata oluştu.");
            rs.AddSystemError($"Araç türleri getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 2. Markalar
    public async Task<IActionResult<List<DotManufacturer>>> GetManufacturersAsync()
    {
        var rs = OperationResult.CreateResult<List<DotManufacturer>>();
        try
        {
            _logger.LogInformation("Tüm markalar getiriliyor...");
            
            var manufacturers = await _context.DbContext.Set<DotManufacturer>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (manufacturers == null || !manufacturers.Any())
            {
                rs.Result = new List<DotManufacturer>();
                rs.AddWarning("Marka bulunamadı.");
                return rs;
            }

            rs.Result = manufacturers;
            _logger.LogInformation($"{manufacturers.Count} marka getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Markalar getirilirken hata oluştu.");
            rs.AddSystemError($"Markalar getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<DotManufacturer>>> GetManufacturersByVehicleTypeAsync(int vehicleType)
    {
        var rs = OperationResult.CreateResult<List<DotManufacturer>>();
        try
        {
            _logger.LogInformation($"VehicleType {vehicleType} için markalar getiriliyor...");
            
            var manufacturers = await _context.DbContext.Set<DotManufacturer>()
                .AsNoTracking()
                .Where(x => x.IsActive && x.VehicleType == vehicleType)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (manufacturers == null || !manufacturers.Any())
            {
                rs.Result = new List<DotManufacturer>();
                rs.AddWarning($"VehicleType {vehicleType} için marka bulunamadı.");
                return rs;
            }

            rs.Result = manufacturers;
            _logger.LogInformation($"{manufacturers.Count} marka getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"VehicleType {vehicleType} için markalar getirilirken hata oluştu.");
            rs.AddSystemError($"Markalar getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 3. Seriler (BaseModels)
    public async Task<IActionResult<List<DotBaseModel>>> GetBaseModelsAsync()
    {
        var rs = OperationResult.CreateResult<List<DotBaseModel>>();
        try
        {
            _logger.LogInformation("Tüm seriler getiriliyor...");
            
            var baseModels = await _context.DbContext.Set<DotBaseModel>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = baseModels ?? new List<DotBaseModel>();
            _logger.LogInformation($"{baseModels?.Count ?? 0} seri getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seriler getirilirken hata oluştu.");
            rs.AddSystemError($"Seriler getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<DotBaseModel>>> GetBaseModelsByManufacturerAsync(string manufacturerKey, int vehicleType)
    {
        var rs = OperationResult.CreateResult<List<DotBaseModel>>();
        try
        {
            _logger.LogInformation($"ManufacturerKey {manufacturerKey} ve VehicleType {vehicleType} için seriler getiriliyor...");
            
            var baseModels = await _context.DbContext.Set<DotBaseModel>()
                .AsNoTracking()
                .Where(x => x.IsActive && 
                           x.ManufacturerKey == manufacturerKey && 
                           x.VehicleType == vehicleType)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = baseModels ?? new List<DotBaseModel>();
            _logger.LogInformation($"{baseModels?.Count ?? 0} seri getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"ManufacturerKey {manufacturerKey} için seriler getirilirken hata oluştu.");
            rs.AddSystemError($"Seriler getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 4. Modeller (SubModels)
    public async Task<IActionResult<List<DotSubModel>>> GetSubModelsAsync()
    {
        var rs = OperationResult.CreateResult<List<DotSubModel>>();
        try
        {
            _logger.LogInformation("Tüm modeller getiriliyor...");
            
            var subModels = await _context.DbContext.Set<DotSubModel>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = subModels ?? new List<DotSubModel>();
            _logger.LogInformation($"{subModels?.Count ?? 0} model getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modeller getirilirken hata oluştu.");
            rs.AddSystemError($"Modeller getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<DotSubModel>>> GetSubModelsByBaseModelAsync(string manufacturerKey, string baseModelKey, int vehicleType)
    {
        var rs = OperationResult.CreateResult<List<DotSubModel>>();
        try
        {
            _logger.LogInformation($"ManufacturerKey {manufacturerKey}, BaseModelKey {baseModelKey} ve VehicleType {vehicleType} için modeller getiriliyor...");
            
            var subModels = await _context.DbContext.Set<DotSubModel>()
                .AsNoTracking()
                .Where(x => x.IsActive && 
                           x.ManufacturerKey == manufacturerKey && 
                           x.BaseModelKey == baseModelKey &&
                           x.VehicleType == vehicleType)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = subModels ?? new List<DotSubModel>();
            _logger.LogInformation($"{subModels?.Count ?? 0} model getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"BaseModelKey {baseModelKey} için modeller getirilirken hata oluştu.");
            rs.AddSystemError($"Modeller getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 5. Kasa Tipleri (CarBodyOptions)
    public async Task<IActionResult<List<DotCarBodyOption>>> GetCarBodyOptionsAsync()
    {
        var rs = OperationResult.CreateResult<List<DotCarBodyOption>>();
        try
        {
            _logger.LogInformation("Tüm kasa tipleri getiriliyor...");
            
            var carBodyOptions = await _context.DbContext.Set<DotCarBodyOption>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = carBodyOptions ?? new List<DotCarBodyOption>();
            _logger.LogInformation($"{carBodyOptions?.Count ?? 0} kasa tipi getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kasa tipleri getirilirken hata oluştu.");
            rs.AddSystemError($"Kasa tipleri getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<DotCarBodyOption>>> GetCarBodyOptionsBySubModelAsync(string manufacturerKey, string baseModelKey, string subModelKey, int vehicleType)
    {
        var rs = OperationResult.CreateResult<List<DotCarBodyOption>>();
        try
        {
            _logger.LogInformation($"SubModelKey {subModelKey} için kasa tipleri getiriliyor...");
            
            var carBodyOptions = await _context.DbContext.Set<DotCarBodyOption>()
                .AsNoTracking()
                .Where(x => x.IsActive && 
                           x.ManufacturerKey == manufacturerKey && 
                           x.BaseModelKey == baseModelKey &&
                           x.SubModelKey == subModelKey &&
                           x.VehicleType == vehicleType)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = carBodyOptions ?? new List<DotCarBodyOption>();
            _logger.LogInformation($"{carBodyOptions?.Count ?? 0} kasa tipi getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"SubModelKey {subModelKey} için kasa tipleri getirilirken hata oluştu.");
            rs.AddSystemError($"Kasa tipleri getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 6. Motor Seçenekleri
    public async Task<IActionResult<List<DotEngineOption>>> GetEngineOptionsAsync()
    {
        var rs = OperationResult.CreateResult<List<DotEngineOption>>();
        try
        {
            _logger.LogInformation("Tüm motor seçenekleri getiriliyor...");
            
            var engineOptions = await _context.DbContext.Set<DotEngineOption>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = engineOptions ?? new List<DotEngineOption>();
            _logger.LogInformation($"{engineOptions?.Count ?? 0} motor seçeneği getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Motor seçenekleri getirilirken hata oluştu.");
            rs.AddSystemError($"Motor seçenekleri getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<DotEngineOption>>> GetEngineOptionsBySubModelAsync(string manufacturerKey, string baseModelKey, string subModelKey, int vehicleType)
    {
        var rs = OperationResult.CreateResult<List<DotEngineOption>>();
        try
        {
            _logger.LogInformation($"SubModelKey {subModelKey} için motor seçenekleri getiriliyor...");
            
            var engineOptions = await _context.DbContext.Set<DotEngineOption>()
                .AsNoTracking()
                .Where(x => x.IsActive && 
                           x.ManufacturerKey == manufacturerKey && 
                           x.BaseModelKey == baseModelKey &&
                           x.SubModelKey == subModelKey &&
                           x.VehicleType == vehicleType)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = engineOptions ?? new List<DotEngineOption>();
            _logger.LogInformation($"{engineOptions?.Count ?? 0} motor seçeneği getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"SubModelKey {subModelKey} için motor seçenekleri getirilirken hata oluştu.");
            rs.AddSystemError($"Motor seçenekleri getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 7. Ek Özellikler (Options)
    public async Task<IActionResult<List<DotOption>>> GetOptionsAsync()
    {
        var rs = OperationResult.CreateResult<List<DotOption>>();
        try
        {
            _logger.LogInformation("Tüm ek özellikler getiriliyor...");
            
            var options = await _context.DbContext.Set<DotOption>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            rs.Result = options ?? new List<DotOption>();
            _logger.LogInformation($"{options?.Count ?? 0} ek özellik getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ek özellikler getirilirken hata oluştu.");
            rs.AddSystemError($"Ek özellikler getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<DotOption>>> GetOptionsBySubModelAsync(string manufacturerKey, string baseModelKey, string subModelKey, int vehicleType)
    {
        var rs = OperationResult.CreateResult<List<DotOption>>();
        try
        {
            _logger.LogInformation($"SubModelKey {subModelKey} için ek özellikler getiriliyor...");
            
            var options = await _context.DbContext.Set<DotOption>()
                .AsNoTracking()
                .Where(x => x.IsActive && 
                           x.ManufacturerKey == manufacturerKey && 
                           x.BaseModelKey == baseModelKey &&
                           x.SubModelKey == subModelKey &&
                           x.VehicleType == vehicleType)
                .OrderBy(x => x.Classification)
                .ThenBy(x => x.Name)
                .ToListAsync();

            rs.Result = options ?? new List<DotOption>();
            _logger.LogInformation($"{options?.Count ?? 0} ek özellik getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"SubModelKey {subModelKey} için ek özellikler getirilirken hata oluştu.");
            rs.AddSystemError($"Ek özellikler getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 8. Araç Fotoğrafları
    public async Task<IActionResult<List<DotVehicleImage>>> GetVehicleImagesAsync()
    {
        var rs = OperationResult.CreateResult<List<DotVehicleImage>>();
        try
        {
            _logger.LogInformation("Tüm araç fotoğrafları getiriliyor...");
            
            var images = await _context.DbContext.Set<DotVehicleImage>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Aspect)
                .ThenBy(x => x.ImageType)
                .ToListAsync();

            rs.Result = images ?? new List<DotVehicleImage>();
            _logger.LogInformation($"{images?.Count ?? 0} araç fotoğrafı getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç fotoğrafları getirilirken hata oluştu.");
            rs.AddSystemError($"Araç fotoğrafları getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<DotVehicleImage>>> GetVehicleImagesByDatECodeAsync(string datECode)
    {
        var rs = OperationResult.CreateResult<List<DotVehicleImage>>();
        try
        {
            _logger.LogInformation($"DatECode {datECode} için araç fotoğrafları getiriliyor...");
            
            var images = await _context.DbContext.Set<DotVehicleImage>()
                .AsNoTracking()
                .Where(x => x.IsActive && x.DatECode == datECode)
                .OrderBy(x => x.Aspect)
                .ThenBy(x => x.ImageType)
                .ToListAsync();

            rs.Result = images ?? new List<DotVehicleImage>();
            _logger.LogInformation($"{images?.Count ?? 0} araç fotoğrafı getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DatECode {datECode} için araç fotoğrafları getirilirken hata oluştu.");
            rs.AddSystemError($"Araç fotoğrafları getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<DotVehicleImage>>> GetVehicleImagesByVehicleInfoAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey)
    {
        var rs = OperationResult.CreateResult<List<DotVehicleImage>>();
        try
        {
            _logger.LogInformation($"VehicleInfo için araç fotoğrafları getiriliyor...");
            
            var images = await _context.DbContext.Set<DotVehicleImage>()
                .AsNoTracking()
                .Where(x => x.IsActive && 
                           x.VehicleType == vehicleType &&
                           x.ManufacturerKey == manufacturerKey && 
                           x.BaseModelKey == baseModelKey &&
                           x.SubModelKey == subModelKey)
                .OrderBy(x => x.Aspect)
                .ThenBy(x => x.ImageType)
                .ToListAsync();

            rs.Result = images ?? new List<DotVehicleImage>();
            _logger.LogInformation($"{images?.Count ?? 0} araç fotoğrafı getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VehicleInfo için araç fotoğrafları getirilirken hata oluştu.");
            rs.AddSystemError($"Araç fotoğrafları getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<List<VehicleImageWithCode>>> GetVehicleImagesByCodesAsync(string vehicleType, string manufacturerKey, string baseModelKey, string subModelKey)
    {
        var rs = OperationResult.CreateResult<List<VehicleImageWithCode>>();
        try
        {
            _logger.LogInformation("Araç görselleri (EF LINQ) getiriliyor - VehicleType: {VehicleType}, ManufacturerKey: {ManufacturerKey}, BaseModelKey: {BaseModelKey}, SubModelKey: {SubModelKey}", vehicleType, manufacturerKey, baseModelKey, subModelKey);

            // Parse VehicleType to int (DotCompiledCode.VehicleType is int)
            // Keys (ManufacturerKey, BaseModelKey, SubModelKey) are strings in DotCompiledCode
            if (!int.TryParse(vehicleType, out var vehicleTypeInt))
            {
                _logger.LogWarning("VehicleType parse edilemedi: {VehicleType}", vehicleType);
                rs.Result = new List<VehicleImageWithCode>();
                return rs;
            }

            // Hard caps to keep queries light-weight
            const int maxProbeCodes   = 150; // number of codes we pull from compiled table before fetching images
            const int maxReturnImages = 12;  // max images to return to UI

            var compiled = _context.DbContext.Set<DotCompiledCode>().AsNoTracking();
            var images   = _context.DbContext.Set<DotVehicleImage>().AsNoTracking();

            // 1) STRICT: fetch a small set of candidate DatECodes filtered by all keys including VehicleType
            var strictCodes = await compiled
                .Where(dc => dc.VehicleType == vehicleTypeInt
                             && dc.ManufacturerKey == manufacturerKey
                             && dc.BaseModelKey    == baseModelKey
                             && dc.SubModelKey     == subModelKey)
                .OrderBy(dc => dc.DatECode)
                .Select(dc => dc.DatECode)
                .Take(maxProbeCodes)
                .ToListAsync();

            Console.WriteLine($"DEBUG: Strict code candidates: {strictCodes.Count}");

            List<VehicleImageWithCode> result = new();

            if (strictCodes.Count > 0)
            {
               
                result = await images
                    .Where(dv => strictCodes.Contains(dv.DatECode) && (!string.IsNullOrEmpty(dv.Url) || !string.IsNullOrEmpty(dv.ImageBase64)))
                    .OrderBy(dv => dv.DatECode)
                    .Select(dv => new VehicleImageWithCode
                    {
                        Url             = dv.Url, // CDN URL (preferred for performance)
                        ImageBase64     = dv.ImageBase64, // Fallback for backward compatibility
                        DatECode        = dv.DatECode,
                        VehicleType     = vehicleType,
                        ManufacturerKey = manufacturerKey,
                        BaseModelKey    = baseModelKey,
                        SubModelKey     = subModelKey
                    })
                    .Take(maxReturnImages)
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Strict images found: {result.Count}");
            }

            // 2) RELAXED: if strict yielded none, try without VehicleType filter
            if (result.Count == 0)
            {
                var relaxedCodes = await compiled
                    .Where(dc => dc.ManufacturerKey == manufacturerKey
                                 && dc.BaseModelKey    == baseModelKey
                                 && dc.SubModelKey     == subModelKey)
                    .OrderBy(dc => dc.DatECode)
                    .Select(dc => dc.DatECode)
                    .Take(maxProbeCodes)
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Relaxed code candidates: {relaxedCodes.Count}");

                if (relaxedCodes.Count > 0)
                {
                    result = await images
                        .Where(dv => relaxedCodes.Contains(dv.DatECode) && (!string.IsNullOrEmpty(dv.Url) || !string.IsNullOrEmpty(dv.ImageBase64)))
                        .OrderBy(dv => dv.DatECode)
                        .Select(dv => new VehicleImageWithCode
                        {
                            Url             = dv.Url, // CDN URL (preferred for performance)
                            ImageBase64     = dv.ImageBase64, // Fallback for backward compatibility
                            DatECode        = dv.DatECode,
                            // echo back requested keys
                            VehicleType     = vehicleType,
                            ManufacturerKey = manufacturerKey,
                            BaseModelKey    = baseModelKey,
                            SubModelKey     = subModelKey
                        })
                        .Take(maxReturnImages)
                        .ToListAsync();

                    Console.WriteLine($"DEBUG: Relaxed images found: {result.Count}");
                }
            }

            rs.Result = result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç görselleri getirilirken hata oluştu. VehicleType: {VehicleType}, ManufacturerKey: {ManufacturerKey}, BaseModelKey: {BaseModelKey}, SubModelKey: {SubModelKey}",
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            rs.AddSystemError($"Araç görselleri getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 9. Derlenmiş Kodlar
    public async Task<IActionResult<List<DotCompiledCode>>> GetCompiledCodesAsync()
    {
        var rs = OperationResult.CreateResult<List<DotCompiledCode>>();
        try
        {
            _logger.LogInformation("Tüm derlenmiş kodlar getiriliyor...");
            
            var compiledCodes = await _context.DbContext.Set<DotCompiledCode>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.LastUsedDate)
                .ToListAsync();

            rs.Result = compiledCodes ?? new List<DotCompiledCode>();
            _logger.LogInformation($"{compiledCodes?.Count ?? 0} derlenmiş kod getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Derlenmiş kodlar getirilirken hata oluştu.");
            rs.AddSystemError($"Derlenmiş kodlar getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<DotCompiledCode?>> GetCompiledCodeByDatECodeAsync(string datECode)
    {
        var rs = OperationResult.CreateResult<DotCompiledCode?>();
        try
        {
            _logger.LogInformation($"DatECode {datECode} için derlenmiş kod getiriliyor...");
            
            var compiledCode = await _context.DbContext.Set<DotCompiledCode>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && x.DatECode == datECode);

            rs.Result = compiledCode;
            if (compiledCode != null)
            {
                _logger.LogInformation($"DatECode {datECode} için derlenmiş kod bulundu.");
            }
            else
            {
                _logger.LogWarning($"DatECode {datECode} için derlenmiş kod bulunamadı.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DatECode {datECode} için derlenmiş kod getirilirken hata oluştu.");
            rs.AddSystemError($"Derlenmiş kod getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion

    #region 10. Yardımcı Metodlar
    public async Task<IActionResult<string?>> GetVehicleImageBase64Async(string datECode, string aspect = "SIDEVIEW")
    {
        var rs = OperationResult.CreateResult<string?>();
        try
        {
            _logger.LogInformation($"DatECode {datECode} ve Aspect {aspect} için araç fotoğrafı getiriliyor...");
            
            var image = await _context.DbContext.Set<DotVehicleImage>()
                .AsNoTracking()
                .Where(x => x.IsActive && 
                           x.DatECode == datECode && 
                           x.Aspect == aspect)
                .FirstOrDefaultAsync();

            if (image != null)
            {
                rs.Result = image.ImageBase64;
                _logger.LogInformation($"DatECode {datECode} için araç fotoğrafı bulundu.");
            }
            else
            {
                _logger.LogWarning($"DatECode {datECode} için araç fotoğrafı bulunamadı.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DatECode {datECode} için araç fotoğrafı getirilirken hata oluştu.");
            rs.AddSystemError($"Araç fotoğrafı getirilirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
    #endregion
}
