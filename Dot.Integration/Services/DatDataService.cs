using Dot.Integration.Abstract;
using Dot.Integration.Dtos;
using ecommerce.Core.Entities;
using Dot.Integration.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Dot.Integration.Services;

public class DatDataService
{
    private readonly IDatService _datService;
    private readonly DbContext _context;
    private readonly ILogger<DatDataService> _logger;

    public DatDataService(IDatService datService, DbContext context, ILogger<DatDataService> logger)
    {
        _datService = datService;
        _context = context;
        _logger = logger;
    }

    public async Task<int?> SaveVehicleTypesAsync(List<DatVehicleType> vehicleTypes)
    {
        try
        {
            int? lastId = null;
            foreach (var dto in vehicleTypes)
            {
                var existing = await _context.Set<DotVehicleType>()
                    .FirstOrDefaultAsync(vt => vt.DatId == dto.Key);
                
                if (existing == null)
                {
                    var newEntity = new DotVehicleType
                    {
                        DatId = dto.Key,
                        Name = dto.Value,
                        Description = dto.Value,
                        CreatedDate = DateTime.UtcNow,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Set<DotVehicleType>().Add(newEntity);
                    await _context.SaveChangesAsync();
                    lastId = newEntity.Id;
                }
                else
                {
                    existing.Name = dto.Value;
                    existing.Description = dto.Value;
                    existing.LastSyncDate = DateTime.UtcNow;
                    lastId = existing.Id;
                }
            }
            
            await _context.SaveChangesAsync();
            return lastId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vehicle types");
            throw;
        }
    }

    public async Task<int?> SaveManufacturersAsync(List<DatManufacturer> manufacturers, int vehicleType)
    {
        try
        {
            int? lastId = null;
            foreach (var dto in manufacturers)
            {
                var existing = await _context.Set<DotManufacturer>()
                    .FirstOrDefaultAsync(m => m.DatKey == dto.Key && m.VehicleType == vehicleType);
                
                if (existing == null)
                {
                    var newEntity = new DotManufacturer
                    {
                        DatKey = dto.Key,
                        Name = dto.Value,
                        VehicleType = vehicleType,
                        CreatedDate = DateTime.UtcNow,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Set<DotManufacturer>().Add(newEntity);
                    await _context.SaveChangesAsync();
                    lastId = newEntity.Id;
                }
                else
                {
                    existing.Name = dto.Value;
                    existing.LastSyncDate = DateTime.UtcNow;
                    lastId = existing.Id;
                }
            }
            
            await _context.SaveChangesAsync();
            return lastId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving manufacturers");
            throw;
        }
    }

    public async Task<int?> SaveBaseModelsAsync(List<DatBaseModel> baseModels, int vehicleType, string manufacturerKey)
    {
        try
        {
            int? lastId = null;
            foreach (var dto in baseModels)
            {
                var existing = await _context.Set<DotBaseModel>()
                    .FirstOrDefaultAsync(bm => bm.DatKey == dto.Key && bm.VehicleType == vehicleType && bm.ManufacturerKey == manufacturerKey);
                
                if (existing == null)
                {
                    var newEntity = new DotBaseModel
                    {
                        DatKey = dto.Key,
                        Name = dto.Value,
                        VehicleType = vehicleType,
                        ManufacturerKey = manufacturerKey,
                        AlternativeBaseType = dto.AlternativeBaseType,
                        RepairIncomplete = dto.RepairIncomplete,
                        CreatedDate = DateTime.UtcNow,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Set<DotBaseModel>().Add(newEntity);
                    await _context.SaveChangesAsync();
                    lastId = newEntity.Id;
                }
                else
                {
                    existing.Name = dto.Value;
                    existing.AlternativeBaseType = dto.AlternativeBaseType;
                    existing.RepairIncomplete = dto.RepairIncomplete;
                    existing.LastSyncDate = DateTime.UtcNow;
                    lastId = existing.Id;
                }
            }
            
            await _context.SaveChangesAsync();
            return lastId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving base models");
            throw;
        }
    }

    public async Task<int?> SaveSubModelsAsync(List<DatSubModel> subModels, int vehicleType, string manufacturerKey, string baseModelKey)
    {
        try
        {
            int? lastId = null;
            foreach (var dto in subModels)
            {
                var existing = await _context.Set<DotSubModel>()
                    .FirstOrDefaultAsync(sm => sm.DatKey == dto.Key && sm.VehicleType == vehicleType 
                        && sm.ManufacturerKey == manufacturerKey && sm.BaseModelKey == baseModelKey);
                
                if (existing == null)
                {
                    var newEntity = new DotSubModel
                    {
                        DatKey = dto.Key,
                        Name = dto.Value,
                        VehicleType = vehicleType,
                        ManufacturerKey = manufacturerKey,
                        BaseModelKey = baseModelKey,
                        CreatedDate = DateTime.UtcNow,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Set<DotSubModel>().Add(newEntity);
                    await _context.SaveChangesAsync();
                    lastId = newEntity.Id;
                }
                else
                {
                    existing.Name = dto.Value;
                    existing.LastSyncDate = DateTime.UtcNow;
                    lastId = existing.Id;
                }
            }
            
            await _context.SaveChangesAsync();
            return lastId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving sub models");
            throw;
        }
    }

    public async Task SaveCompiledCodeAsync(string datECode, int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, List<string>? selectedOptions = null)
    {
        try
        {
            var existing = await _context.Set<DotCompiledCode>()
                .FirstOrDefaultAsync(cc => cc.DatECode == datECode);
            
            var optionsString = selectedOptions != null && selectedOptions.Any() 
                ? string.Join(",", selectedOptions) 
                : null;
            
            if (existing == null)
            {
                _context.Set<DotCompiledCode>().Add(new DotCompiledCode
                {
                    DatECode = datECode,
                    VehicleType = vehicleType,
                    ManufacturerKey = manufacturerKey,
                    BaseModelKey = baseModelKey,
                    SubModelKey = subModelKey,
                    SelectedOptions = optionsString,
                    CreatedDate = DateTime.UtcNow,
                    LastUsedDate = DateTime.UtcNow,
                    IsActive = true
                });
            }
            else
            {
                existing.LastUsedDate = DateTime.UtcNow;
                existing.SelectedOptions = optionsString;
            }
            
            await _context.SaveChangesAsync();
            // Success log removed - only errors logged
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving compiled code");
            throw;
        }
    }

    public async Task SaveOptionsAsync(List<DatOption> options, int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, int classification)
    {
        try
        {
            foreach (var dto in options)
            {
                var existing = await _context.Set<DotOption>()
                    .FirstOrDefaultAsync(o => o.DatKey == dto.Key && o.VehicleType == vehicleType 
                        && o.ManufacturerKey == manufacturerKey && o.BaseModelKey == baseModelKey 
                        && o.SubModelKey == subModelKey && o.Classification == classification);
                
                if (existing == null)
                {
                    _context.Set<DotOption>().Add(new DotOption
                    {
                        DatKey = dto.Key,
                        Name = dto.Value,
                        VehicleType = vehicleType,
                        ManufacturerKey = manufacturerKey,
                        BaseModelKey = baseModelKey,
                        SubModelKey = subModelKey,
                        Classification = classification,
                        CreatedDate = DateTime.UtcNow,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    });
                }
                else
                {
                    existing.Name = dto.Value;
                    existing.LastSyncDate = DateTime.UtcNow;
                }
            }
            
            await _context.SaveChangesAsync();
            // Success log removed - only errors logged
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving options");
            throw;
        }
    }

    public async Task SaveEngineOptionsAsync(List<DatEngineOption> engineOptions, int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey)
    {
        try
        {
            foreach (var dto in engineOptions)
            {
                var existing = await _context.Set<DotEngineOption>()
                    .FirstOrDefaultAsync(eo => eo.DatKey == dto.Key && eo.VehicleType == vehicleType 
                        && eo.ManufacturerKey == manufacturerKey && eo.BaseModelKey == baseModelKey 
                        && eo.SubModelKey == subModelKey);
                
                if (existing == null)
                {
                    _context.Set<DotEngineOption>().Add(new DotEngineOption
                    {
                        DatKey = dto.Key,
                        Name = dto.Value,
                        VehicleType = vehicleType,
                        ManufacturerKey = manufacturerKey,
                        BaseModelKey = baseModelKey,
                        SubModelKey = subModelKey,
                        CreatedDate = DateTime.UtcNow,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    });
                }
                else
                {
                    existing.Name = dto.Value;
                    existing.LastSyncDate = DateTime.UtcNow;
                }
            }
            
            await _context.SaveChangesAsync();
            // Success log removed - only errors logged
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving engine options");
            throw;
        }
    }

    public async Task SaveCarBodyOptionsAsync(List<DatCarBodyOption> carBodyOptions, int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey)
    {
        try
        {
            foreach (var dto in carBodyOptions)
            {
                var existing = await _context.Set<DotCarBodyOption>()
                    .FirstOrDefaultAsync(cb => cb.DatKey == dto.Key && cb.VehicleType == vehicleType 
                        && cb.ManufacturerKey == manufacturerKey && cb.BaseModelKey == baseModelKey 
                        && cb.SubModelKey == subModelKey);
                
                if (existing == null)
                {
                    _context.Set<DotCarBodyOption>().Add(new DotCarBodyOption
                    {
                        DatKey = dto.Key,
                        Name = dto.Value,
                        VehicleType = vehicleType,
                        ManufacturerKey = manufacturerKey,
                        BaseModelKey = baseModelKey,
                        SubModelKey = subModelKey,
                        CreatedDate = DateTime.UtcNow,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    });
                }
                else
                {
                    existing.Name = dto.Value;
                    existing.LastSyncDate = DateTime.UtcNow;
                }
            }
            
            await _context.SaveChangesAsync();
            // Success log removed - only errors logged
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving car body options");
            throw;
        }
    }

    // SaveVehiclesAsync removed - DotVehicle entity no longer used, replaced by DotVehicleData

    public async Task SaveTokenCacheAsync(string token, DateTime expiry)
    {
        try
        {
            var existing = await _context.Set<DotTokenCache>()
                .FirstOrDefaultAsync();
            
            if (existing == null)
            {
                _context.Set<DotTokenCache>().Add(new DotTokenCache
                {
                    Token = token,
                    ExpiresAt = expiry,
                    CreatedDate = DateTime.UtcNow,
                    LastUsedDate = DateTime.UtcNow,
                    IsActive = true
                });
            }
            else
            {
                existing.Token = token;
                existing.ExpiresAt = expiry;
                existing.LastUsedDate = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            // Success log removed - only errors logged
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving token cache");
            throw;
        }
    }

    public async Task SaveVehicleAsync(
        int vehicleTypeId,
        string manufacturerName,
        string baseModelName,
        string subModelName,
        string? engineInfo = null,
        string? fuelType = null,
        string? year = null)
    {
        try
        {
            // DatId = Hash of VehicleType-Manufacturer-BaseModel-SubModel (max 50 chars)
            var combinedKey = $"{vehicleTypeId}-{manufacturerName}-{baseModelName}-{subModelName}";
            var datId = GenerateShortId(combinedKey);
            
            var existing = await _context.Set<DotVehicle>()
                .FirstOrDefaultAsync(v => v.DatId == datId);
            
            if (existing == null)
            {
                _context.Set<DotVehicle>().Add(new DotVehicle
                {
                    DatId = datId,
                    Make = manufacturerName,
                    Model = $"{baseModelName} {subModelName}",
                    Year = year,
                    Engine = engineInfo,
                    FuelType = fuelType,
                    DotVehicleTypeId = vehicleTypeId,
                    CreatedDate = DateTime.UtcNow,
                    LastSyncDate = DateTime.UtcNow,
                    IsActive = true
                });
            }
            else
            {
                existing.Make = manufacturerName;
                existing.Model = $"{baseModelName} {subModelName}";
                existing.Year = year;
                existing.Engine = engineInfo;
                existing.FuelType = fuelType;
                existing.LastSyncDate = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vehicle");
            throw;
        }
    }

    /// <summary>
    /// DatData tablosundan VehicleType, Manufacturer, BaseModel'e göre DatProcessNo listesi döndürür
    /// </summary>
    public async Task<List<int>> GetDatProcessNumbersByVehicleAsync(
        int vehicleType, 
        int manufacturerKey, 
        int baseModelKey)
    {
        try
        {
            var datProcessNos = await _context.Set<DatData>()
                .Where(d => d.VehicleTypeKey == vehicleType 
                         && d.ManufactureKey == manufacturerKey 
                         && d.BaseModelKey == baseModelKey
                         && !d.IsTrans) // 🎯 SADECE aktarılmayanları al!
                .Select(d => d.DatProcessNo)
                .Distinct()
                .ToListAsync();
            
            _logger.LogInformation("📋 Found {Count} DatProcessNos for VT:{VT} M:{M} B:{B} (IsTrans=false)", 
                datProcessNos.Count, vehicleType, manufacturerKey, baseModelKey);
            
            return datProcessNos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DatProcessNos for VT:{VT} M:{M} B:{B}", 
                vehicleType, manufacturerKey, baseModelKey);
            return new List<int>();
        }
    }

    /// <summary>
    /// DatData tablosundaki tüm kayıtları getirir
    /// </summary>
    public async Task<List<DatData>> GetAllDatDataAsync()
    {
        try
        {
            return await _context.Set<DatData>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all DatData records");
            return new List<DatData>();
        }
    }

    /// <summary>
    /// DatData tablosundan sadece UNIQUE VT-M-B kombinasyonlarını getirir (SADECE IsTrans = false olanları!)
    /// </summary>
    public async Task<List<(int VehicleTypeKey, int ManufactureKey, int BaseModelKey)>> GetUniqueDatDataGroupsAsync()
    {
        try
        {
            return await _context.Set<DatData>()
                .Where(d => !d.IsTrans) // 🎯 SADECE aktarılmayanları al!
                .Select(d => new { d.VehicleTypeKey, d.ManufactureKey, d.BaseModelKey })
                .Distinct()
                .Select(g => new ValueTuple<int, int, int>(g.VehicleTypeKey, g.ManufactureKey, g.BaseModelKey))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unique DatData groups");
            return new List<(int, int, int)>();
        }
    }

    /// <summary>
    /// Bu VT-M-B için kullanılan DatProcessNo'ları IsTrans = true olarak işaretle
    /// </summary>
    public async Task MarkDatDataAsTransferredAsync(
        int vehicleType, 
        int manufacturerKey, 
        int baseModelKey, 
        List<string> processedDatProcessNos)
    {
        try
        {
            var processNoInts = processedDatProcessNos.Select(p => int.Parse(p)).ToList();
            
            _logger.LogDebug("🔍 Searching for DatData records: VT:{VT} M:{M} B:{B}, ProcessNos: {ProcessNos}", 
                vehicleType, manufacturerKey, baseModelKey, string.Join(",", processNoInts.Take(5)));
            
            // Önce tüm kayıtları kontrol et (IsTrans durumu ne olursa olsun)
            var allRecords = await _context.Set<DatData>()
                .Where(d => d.VehicleTypeKey == vehicleType 
                         && d.ManufactureKey == manufacturerKey 
                         && d.BaseModelKey == baseModelKey
                         && processNoInts.Contains(d.DatProcessNo))
                .ToListAsync();
            
            _logger.LogInformation("🔍 TOTAL records found: {Count}", allRecords.Count);
            foreach (var rec in allRecords.Take(3))
            {
                _logger.LogInformation("📋 Record: DPN={DPN}, IsTrans={IsTrans}, Id={Id}", 
                    rec.DatProcessNo, rec.IsTrans, rec.Id);
            }
            
            // Şimdi sadece false olanları filtrele
            var recordsToUpdate = allRecords.Where(d => !d.IsTrans).ToList();
            
            _logger.LogDebug("📝 Found {Count} records to update to IsTrans=true", recordsToUpdate.Count);
            
            // ID'leri topla ve parametreli SQL ile güvenli update
            var idsToUpdate = recordsToUpdate.Select(r => r.Id).ToList();
            
            int changesCount = 0;
            if (idsToUpdate.Any())
            {
                // Parametreli SQL ile güvenli update
                var idList = string.Join(",", idsToUpdate);
                var sql = $@"UPDATE ""DatDatas"" SET ""IsTrans"" = true WHERE ""Id"" IN ({idList})";
                
                changesCount = await _context.Database.ExecuteSqlRawAsync(sql);
                _logger.LogInformation("💾 RAW SQL Update completed. {ChangesCount} rows affected", changesCount);
            }
            else
            {
                _logger.LogWarning("⚠️ No records found to update");
            }
            
            _logger.LogInformation("✅ {Count} DatData kaydı IsTrans = true olarak işaretlendi (VT:{VT} M:{M} B:{B})", 
                recordsToUpdate.Count, vehicleType, manufacturerKey, baseModelKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking DatData as transferred for VT:{VT} M:{M} B:{B}", 
                vehicleType, manufacturerKey, baseModelKey);
        }
    }

    public async Task SavePartsAsync(List<DatPartSimple> parts, string? vehicleDatId = null)
    {
        try
        {
            if (!parts.Any())
            {
                _logger.LogDebug("No parts to save");
                return;
            }

            // 1. Önce mevcut part'ları toplu çek (BULK QUERY)
            var partNumbers = parts.Select(p => p.PartNumber).Distinct().ToList();
            
            var existingPartsQuery = await _context.Set<DotPart>()
                .Where(p => partNumbers.Contains(p.PartNumber))
                .ToListAsync();
            
            // COMPOSITE KEY: PartNumber + VehicleType + BaseModelKey + DatProcessNumber
            var existingParts = existingPartsQuery
                .GroupBy(p => new { 
                    p.PartNumber, 
                    p.VehicleType, 
                    p.BaseModelKey, 
                    p.DatProcessNumber 
                })
                .ToDictionary(
                    g => g.Key, 
                    g => g.First()); // Duplicate varsa ilk kaydı al
            
            _logger.LogDebug("🔍 Found {ExistingCount} existing parts (composite key), Total incoming: {TotalCount}", 
                existingParts.Count, parts.Count);

            var newParts = new List<DotPart>();
            var updatedCount = 0;
            var skippedCount = 0;
            
            // Duplicate önleme: API'den aynı batch'te duplicate gelebilir
            var processedKeys = new HashSet<string>();

            // 2. Yeni ve güncellenecekleri ayır
            foreach (var dto in parts)
            {
                // COMPOSITE UNIQUE KEY
                var uniqueKey = $"{dto.PartNumber}_{dto.VehicleType}_{dto.BaseModelKey}_{dto.DatProcessNumber}";
                
                if (!processedKeys.Add(uniqueKey))
                {
                    _logger.LogDebug("⚠️ Duplicate part atlandı: {PartNumber} (VT:{VT}, BM:{BM}, DPN:{DPN})", 
                        dto.PartNumber, dto.VehicleType, dto.BaseModelKey, dto.DatProcessNumber);
                    skippedCount++;
                    continue;
                }
                
                // Composite key ile mevcut kaydı kontrol et
                var compositeKey = new { 
                    PartNumber = dto.PartNumber, 
                    VehicleType = dto.VehicleType, 
                    BaseModelKey = dto.BaseModelKey, 
                    DatProcessNumber = dto.DatProcessNumber 
                };
                
                if (existingParts.TryGetValue(compositeKey, out var existing))
                {
                    // UPDATE
                    existing.Description = dto.Description ?? existing.Description;
                    existing.Name = dto.Name ?? existing.Name;
                    existing.NetPrice = dto.NetPrice;
                    existing.Availability = dto.Availability;
                    existing.PriceDate = dto.PriceDate;
                    existing.WorkTimeMin = dto.WorkTimeMin;
                    existing.WorkTimeMax = dto.WorkTimeMax;
                    existing.DatProcessNumber = dto.DatProcessNumber ?? existing.DatProcessNumber;
                    existing.VehicleType = dto.VehicleType ?? existing.VehicleType;
                    existing.VehicleTypeName = dto.VehicleTypeName ?? existing.VehicleTypeName;
                    existing.ManufacturerKey = dto.ManufacturerKey ?? existing.ManufacturerKey;
                    existing.ManufacturerName = dto.ManufacturerName ?? existing.ManufacturerName;
                    existing.BaseModelKey = dto.BaseModelKey ?? existing.BaseModelKey;
                    existing.BaseModelName = dto.BaseModelName ?? existing.BaseModelName;
                    existing.DescriptionIdentifier = dto.DescriptionIdentifier ?? existing.DescriptionIdentifier;
                    existing.SubModelsJson = dto.SubModelsJson ?? existing.SubModelsJson;
                    existing.PreviousPricesJson = dto.PreviousPricesJson ?? existing.PreviousPricesJson;
                    existing.PreviousPartNumbersJson = dto.PreviousPartNumbersJson ?? existing.PreviousPartNumbersJson;
                    existing.DatVehicleId = vehicleDatId;
                    existing.LastSyncDate = DateTime.UtcNow;
                    updatedCount++;
                }
                else
                {
                    // INSERT - listeye ekle, toplu insert için
                    newParts.Add(new DotPart
                    {
                        PartNumber = dto.PartNumber,
                        Description = dto.Description ?? string.Empty,
                        Name = dto.Name,
                        NetPrice = dto.NetPrice,
                        Availability = dto.Availability,
                        PriceDate = dto.PriceDate,
                        WorkTimeMin = dto.WorkTimeMin,
                        WorkTimeMax = dto.WorkTimeMax,
                        DatProcessNumber = dto.DatProcessNumber,
                        VehicleType = dto.VehicleType,
                        VehicleTypeName = dto.VehicleTypeName,
                        ManufacturerKey = dto.ManufacturerKey,
                        ManufacturerName = dto.ManufacturerName,
                        BaseModelKey = dto.BaseModelKey,
                        BaseModelName = dto.BaseModelName,
                        DescriptionIdentifier = dto.DescriptionIdentifier,
                        SubModelsJson = dto.SubModelsJson,
                        PreviousPricesJson = dto.PreviousPricesJson,
                        PreviousPartNumbersJson = dto.PreviousPartNumbersJson,
                        DatVehicleId = vehicleDatId,
                        CreatedDate = DateTime.UtcNow,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }

            // 3. BULK INSERT (tek SaveChanges)
            if (newParts.Any())
            {
                await _context.Set<DotPart>().AddRangeAsync(newParts);
            }
            
            // 4. TEK SaveChanges (tüm insert + update)
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("✅ Saved {Count} parts to database ({NewCount} new, {UpdatedCount} updated, {SkippedCount} duplicates skipped)", 
                parts.Count, newParts.Count, updatedCount, skippedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving parts");
            throw;
        }
    }

    public async Task SaveVehicleImagesAsync(string datECode, List<DatVehicleImageDto> images)
    {
        try
        {
            foreach (var imageDto in images)
            {
                // Check if image already exists
                var existing = await _context.Set<DotVehicleImage>()
                    .FirstOrDefaultAsync(i => 
                        i.DatECode == datECode && 
                        i.Aspect == imageDto.Aspect && 
                        i.ImageType == imageDto.ImageType);
                
                if (existing == null)
                {
                    
                    var newImage = new DotVehicleImage
                    {
                        DatECode = datECode,
                        Aspect = imageDto.Aspect,
                        ImageType = imageDto.ImageType,
                        ImageFormat = imageDto.ImageFormat,
                        ImageBase64 = imageDto.ImageBase64,
                        LastSyncDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    
                    await _context.Set<DotVehicleImage>().AddAsync(newImage);
                }
                else
                {
                   
                    existing.ImageBase64 = imageDto.ImageBase64;
                    existing.ImageFormat = imageDto.ImageFormat;
                    existing.LastSyncDate = DateTime.UtcNow;
                }
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Saved {Count} vehicle images for datECode {DatECode}", images.Count, datECode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving vehicle images for datECode {DatECode}", datECode);
            throw;
        }
    }

    public async Task SaveVehicleDataAsync(DatVehicleDataVehicle vehicle)
    {
        try
        {
            var tech = vehicle.TechInfo;
            var price = vehicle.OriginalPriceInfo;

            var existing = await _context.Set<DotVehicleData>()
                .FirstOrDefaultAsync(x => x.DatECode == vehicle.DatECode);

            if (existing == null)
            {
                var entity = new DotVehicleData
                {
                    DatECode = vehicle.DatECode,
                    Container = vehicle.Container,
                    ConstructionTime = vehicle.ConstructionTime,
                    ContainerName = vehicle.ContainerNameN ?? vehicle.ContainerName,
                    VehicleTypeName = vehicle.VehicleTypeNameN ?? vehicle.VehicleTypeName,
                    ManufacturerName = vehicle.ManufacturerName,
                    BaseModelName = vehicle.BaseModelName,
                    SubModelName = vehicle.SubModelName,
                    VehicleType = vehicle.VehicleType,
                    Manufacturer = vehicle.Manufacturer,
                    BaseModel = vehicle.BaseModel,
                    SubModel = vehicle.SubModel,
                    KbaNumbers = vehicle.KbaNumbers,
                    OriginalPriceNet = vehicle.OriginalPriceNet,
                    OriginalPriceGross = vehicle.OriginalPriceGross,
                    OriginalPriceVATRate = price?.OriginalPriceVATRate,
                    RentalCarClass = vehicle.RentalCarClass,
                    PowerHp = tech?.PowerHp ?? vehicle.PowerHp,
                    PowerKw = tech?.PowerKw ?? vehicle.PowerKw,
                    Capacity = tech?.Capacity ?? vehicle.Capacity,
                    FuelMethod = tech?.FuelMethod ?? vehicle.FuelMethod,
                    GearboxType = tech?.GearboxType ?? vehicle.GearboxType,
                    Length = tech?.Length ?? vehicle.Length,
                    Width = tech?.Width ?? vehicle.Width,
                    Height = tech?.Height ?? vehicle.Height,
                    StructureType = tech?.StructureType,
                    StructureDescription = tech?.StructureDescription,
                    CountOfAxles = tech?.CountOfAxles,
                    CountOfDrivedAxles = tech?.CountOfDrivedAxles,
                    WheelBase = tech?.WheelBase,
                    RoofLoad = tech?.RoofLoad,
                    TrailerLoadBraked = tech?.TrailerLoadBraked,
                    TrailerLoadUnbraked = tech?.TrailerLoadUnbraked,
                    VehicleSeats = tech?.VehicleSeats,
                    VehicleDoors = tech?.VehicleDoors,
                    CountOfAirbags = tech?.CountOfAirbags,
                    Acceleration = tech?.Acceleration,
                    SpeedMax = tech?.SpeedMax,
                    Cylinder = tech?.Cylinder,
                    CylinderArrangement = tech?.CylinderArrangement,
                    RotationsOnMaxPower = tech?.RotationsOnMaxPower,
                    RotationsOnMaxTorque = tech?.RotationsOnMaxTorque,
                    Torque = tech?.Torque,
                    NrOfGears = tech?.NrOfGears,
                    OriginalTireSizeAxle1 = tech?.OriginalTireSizeAxle1,
                    OriginalTireSizeAxle2 = tech?.OriginalTireSizeAxle2,
                    TankVolume = tech?.TankVolume,
                    ConsumptionInTown = tech?.ConsumptionInTown,
                    ConsumptionOutOfTown = tech?.ConsumptionOutOfTown,
                    Consumption = tech?.Consumption,
                    Co2Emission = tech?.Co2Emission,
                    EmissionClass = tech?.EmissionClass,
                    Drive = tech?.Drive,
                    DriveCode = tech?.DriveCode,
                    EngineCycle = tech?.EngineCycle,
                    FuelMethodCode = tech?.FuelMethodCode,
                    FuelMethodType = tech?.FuelMethodType,
                    UnloadedWeight = tech?.UnloadedWeight,
                    PermissableTotalWeight = tech?.PermissableTotalWeight,
                    LoadingSpace = tech?.LoadingSpace,
                    LoadingSpaceMax = tech?.LoadingSpaceMax,
                    InsuranceTypeClassLiability = tech?.InsuranceTypeClassLiability,
                    InsuranceTypeClassCascoPartial = tech?.InsuranceTypeClassCascoPartial,
                    InsuranceTypeClassCascoComplete = tech?.InsuranceTypeClassCascoComplete,
                    ProductGroupName = tech?.ProductGroupName,
                    LastSyncDate = DateTime.UtcNow,
                    IsActive = true
                };
                await _context.Set<DotVehicleData>().AddAsync(entity);
            }
            else
            {
                existing.Container = vehicle.Container;
                existing.ConstructionTime = vehicle.ConstructionTime;
                existing.ContainerName = vehicle.ContainerNameN ?? vehicle.ContainerName;
                existing.VehicleTypeName = vehicle.VehicleTypeNameN ?? vehicle.VehicleTypeName;
                existing.ManufacturerName = vehicle.ManufacturerName;
                existing.BaseModelName = vehicle.BaseModelName;
                existing.SubModelName = vehicle.SubModelName;
                existing.VehicleType = vehicle.VehicleType;
                existing.Manufacturer = vehicle.Manufacturer;
                existing.BaseModel = vehicle.BaseModel;
                existing.SubModel = vehicle.SubModel;
                existing.KbaNumbers = vehicle.KbaNumbers;
                existing.OriginalPriceNet = vehicle.OriginalPriceNet;
                existing.OriginalPriceGross = vehicle.OriginalPriceGross;
                existing.OriginalPriceVATRate = price?.OriginalPriceVATRate;
                existing.RentalCarClass = vehicle.RentalCarClass;
                existing.PowerHp = tech?.PowerHp ?? vehicle.PowerHp;
                existing.PowerKw = tech?.PowerKw ?? vehicle.PowerKw;
                existing.Capacity = tech?.Capacity ?? vehicle.Capacity;
                existing.FuelMethod = tech?.FuelMethod ?? vehicle.FuelMethod;
                existing.GearboxType = tech?.GearboxType ?? vehicle.GearboxType;
                existing.Length = tech?.Length ?? vehicle.Length;
                existing.Width = tech?.Width ?? vehicle.Width;
                existing.Height = tech?.Height ?? vehicle.Height;
                existing.StructureType = tech?.StructureType;
                existing.StructureDescription = tech?.StructureDescription;
                existing.CountOfAxles = tech?.CountOfAxles;
                existing.CountOfDrivedAxles = tech?.CountOfDrivedAxles;
                existing.WheelBase = tech?.WheelBase;
                existing.RoofLoad = tech?.RoofLoad;
                existing.TrailerLoadBraked = tech?.TrailerLoadBraked;
                existing.TrailerLoadUnbraked = tech?.TrailerLoadUnbraked;
                existing.VehicleSeats = tech?.VehicleSeats;
                existing.VehicleDoors = tech?.VehicleDoors;
                existing.CountOfAirbags = tech?.CountOfAirbags;
                existing.Acceleration = tech?.Acceleration;
                existing.SpeedMax = tech?.SpeedMax;
                existing.Cylinder = tech?.Cylinder;
                existing.CylinderArrangement = tech?.CylinderArrangement;
                existing.RotationsOnMaxPower = tech?.RotationsOnMaxPower;
                existing.RotationsOnMaxTorque = tech?.RotationsOnMaxTorque;
                existing.Torque = tech?.Torque;
                existing.NrOfGears = tech?.NrOfGears;
                existing.OriginalTireSizeAxle1 = tech?.OriginalTireSizeAxle1;
                existing.OriginalTireSizeAxle2 = tech?.OriginalTireSizeAxle2;
                existing.TankVolume = tech?.TankVolume;
                existing.ConsumptionInTown = tech?.ConsumptionInTown;
                existing.ConsumptionOutOfTown = tech?.ConsumptionOutOfTown;
                existing.Consumption = tech?.Consumption;
                existing.Co2Emission = tech?.Co2Emission;
                existing.EmissionClass = tech?.EmissionClass;
                existing.Drive = tech?.Drive;
                existing.DriveCode = tech?.DriveCode;
                existing.EngineCycle = tech?.EngineCycle;
                existing.FuelMethodCode = tech?.FuelMethodCode;
                existing.FuelMethodType = tech?.FuelMethodType;
                existing.UnloadedWeight = tech?.UnloadedWeight;
                existing.PermissableTotalWeight = tech?.PermissableTotalWeight;
                existing.LoadingSpace = tech?.LoadingSpace;
                existing.LoadingSpaceMax = tech?.LoadingSpaceMax;
                existing.InsuranceTypeClassLiability = tech?.InsuranceTypeClassLiability;
                existing.InsuranceTypeClassCascoPartial = tech?.InsuranceTypeClassCascoPartial;
                existing.InsuranceTypeClassCascoComplete = tech?.InsuranceTypeClassCascoComplete;
                existing.ProductGroupName = tech?.ProductGroupName;
                existing.LastSyncDate = DateTime.UtcNow;
                existing.IsActive = true;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DotVehicleData kayıt hatası: {DatECode}", vehicle.DatECode);
            throw;
        }
    }

    private static string GenerateShortId(string input)
    {
        // MD5 hash kullanarak 32 karakterlik benzersiz ID oluştur
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// DotCompiledCodes tablosundan mevcut DatECode'u getirir
    /// </summary>
    public async Task<string?> GetExistingDatECodeAsync(
        int vehicleType, 
        string manufacturerKey, 
        string baseModelKey, 
        string subModelKey)
    {
        try
        {
            var datECode = await _context.Set<DotCompiledCode>()
                .Where(c => c.VehicleType == vehicleType 
                         && c.ManufacturerKey == manufacturerKey 
                         && c.BaseModelKey == baseModelKey 
                         && c.SubModelKey == subModelKey)
                .OrderByDescending(c => c.CreatedDate) // En yeni olanı al
                .Select(c => c.DatECode)
                .FirstOrDefaultAsync();
            
            return datECode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting existing DatECode for VT:{VT} M:{M} B:{B} S:{S}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            return null;
        }
    }

    /// <summary>
    /// Classification bazında options'ları getirir (her classification'dan tüm options)
    /// </summary>
    public async Task<Dictionary<int, List<string>>> GetOptionsGroupedByClassificationAsync(
        int vehicleType,
        string manufacturerKey,
        string baseModelKey,
        string subModelKey)
    {
        try
        {
            var options = await _context.Set<DotOption>()
                .Where(o => o.VehicleType == vehicleType
                         && o.ManufacturerKey == manufacturerKey
                         && o.BaseModelKey == baseModelKey
                         && o.SubModelKey == subModelKey)
                .GroupBy(o => o.Classification)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(o => o.DatKey).ToList());
            
            _logger.LogDebug("📊 Found {Count} classifications with options for VT:{VT} M:{M} B:{B} S:{S}",
                options.Count, vehicleType, manufacturerKey, baseModelKey, subModelKey);
            
            return options;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting options by classification for VT:{VT} M:{M} B:{B} S:{S}",
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            return new Dictionary<int, List<string>>();
        }
    }

}
