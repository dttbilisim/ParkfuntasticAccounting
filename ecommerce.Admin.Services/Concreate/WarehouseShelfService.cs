using AutoMapper;
using ecommerce.Admin.Domain.Dtos.WarehouseShelfDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities.Warehouse;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Concreate
{
    public class WarehouseShelfService : IWarehouseShelfService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<WarehouseShelf> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<WarehouseShelfService> _logger;

        public WarehouseShelfService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger<WarehouseShelfService> logger)
        {
            _context = context;
            _repository = context.GetRepository<WarehouseShelf>();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult<Paging<List<WarehouseShelfListDto>>>> GetShelves(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<WarehouseShelfListDto>>>();

            try
            {
                response.Result = await _repository.GetAll(true)
                    .Include(s => s.Warehouse)
                    .Where(s => s.Status != (int)EntityStatus.Deleted)
                    .ToPagedResultAsync<WarehouseShelfListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetShelves Exception " + e);
                response.AddSystemError(e.Message);
            }

            return response;
        }

        public async Task<IActionResult<List<WarehouseShelfListDto>>> GetShelvesByWarehouse(int warehouseId)
        {
             var response = new IActionResult<List<WarehouseShelfListDto>> { Result = new() };
            try
            {
                var shelves = await _repository.GetAllAsync(
                    predicate: f => f.WarehouseId == warehouseId && f.Status == (int)EntityStatus.Active, 
                    include: i => i.Include(x => x.Warehouse),
                    disableTracking: true);
                
                response.Result = _mapper.Map<List<WarehouseShelfListDto>>(shelves);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetShelvesByWarehouse Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertShelf(AuditWrapDto<WarehouseShelfUpsertDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                
                if (!dto.Id.HasValue || dto.Id == 0)
                {
                    // Check if exists (including deleted ones) to avoid unique constraint violation
                    var existingShelf = await _context.DbContext.WarehouseShelves.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId && x.Code == dto.Code);

                    if (existingShelf != null)
                    {
                        if (existingShelf.Status == (int)EntityStatus.Deleted)
                        {
                             // Undelete and Update
                            existingShelf.Description = dto.Description;
                            existingShelf.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                            existingShelf.ModifiedId = model.UserId;
                            existingShelf.ModifiedDate = DateTime.Now;
                            rs.AddSuccess("Raf geri yüklendi ve güncellendi.");
                        }
                        else
                        {
                            rs.AddError("Bu raf kodu zaten kullanımda.");
                            return rs;
                        }
                    }
                    else
                    {
                        var entity = _mapper.Map<WarehouseShelf>(dto);
                        entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                        entity.CreatedId = model.UserId;
                        entity.CreatedDate = DateTime.Now;
                        await _repository.InsertAsync(entity);
                        rs.AddSuccess("Raf eklendi.");
                    }
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if (current == null)
                    {
                        rs.AddError("Raf bulunamadı");
                        return rs;
                    }

                    // Check for duplicate code on update
                    var duplicateCheck = await _context.DbContext.WarehouseShelves.IgnoreQueryFilters()
                         .AnyAsync(x => x.WarehouseId == dto.WarehouseId && x.Code == dto.Code && x.Id != dto.Id);
                    
                    if (duplicateCheck) 
                    {
                         rs.AddError("Bu raf kodu başka bir raf tarafından kullanılıyor.");
                         return rs;
                    }

                    var updated = _mapper.Map<WarehouseShelf>(dto);
                    updated.Id = current.Id;
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                    rs.AddSuccess("Raf güncellendi.");
                }

                await _context.SaveChangesAsync();
                 var lastResult = _context.LastSaveChangesResult;
                 if (!lastResult.IsOk)
                {
                    if (lastResult.Exception != null)
                         rs.AddError(lastResult.Exception.ToString());
                    else
                         rs.AddError("Kayıt sırasında hata oluştu.");
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertShelf Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteShelf(AuditWrapDto<WarehouseShelfDeleteDto> model)
        {
             var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // Check dependencies (Product Stocks)
                var stockExists = await _context.DbContext.ProductStocks.AsNoTracking()
                    .AnyAsync(x => x.WarehouseShelfId == model.Dto.Id && x.Quantity > 0 && x.Status != (int)EntityStatus.Deleted);
                
                if (stockExists)
                {
                    rs.AddError("Bu rafta ürün stokları var. Önce stokları boşaltmalısınız.");
                    return rs;
                }

                await _context.DbContext.WarehouseShelves.Where(f => f.Id == model.Dto.Id).
                    ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).
                    SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                rs.AddSuccess("Raf silindi");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteShelf Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<WarehouseShelfUpsertDto>> GetShelfById(int Id)
        {
             var rs = new IActionResult<WarehouseShelfUpsertDto> { Result = new WarehouseShelfUpsertDto() };
            try
            {
                var entity = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var dto = _mapper.Map<WarehouseShelfUpsertDto>(entity);
                if (dto != null)
                {
                    dto.StatusBool = dto.Status == (int)EntityStatus.Active;
                    rs.Result = dto;
                }
                else rs.AddError("Raf Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetShelfById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> BatchCreateShelves(AuditWrapDto<WarehouseShelfBatchCreateDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                var createdCount = 0;
                var skippedCount = 0;

                for (int i = dto.StartNumber; i <= dto.EndNumber; i++)
                {
                    var code = $"{dto.Prefix}{i}{dto.Suffix}";
                    
                    // Check if exists (including deleted ones)
                    var existingShelf = await _context.DbContext.WarehouseShelves.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId && x.Code == code);

                    if (existingShelf == null)
                    {
                        var entity = new WarehouseShelf
                        {
                            WarehouseId = dto.WarehouseId,
                            Code = code,
                            Description = dto.Description ?? "Toplu Oluşturuldu",
                            Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive,
                            CreatedId = model.UserId,
                            CreatedDate = DateTime.Now
                        };
                        await _context.DbContext.WarehouseShelves.AddAsync(entity);
                        createdCount++;
                    }
                    else if (existingShelf.Status == (int)EntityStatus.Deleted)
                    {
                        // Undelete
                        existingShelf.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                        existingShelf.ModifiedId = model.UserId;
                        existingShelf.ModifiedDate = DateTime.Now;
                        existingShelf.Description = dto.Description ?? "Toplu Oluşturuldu (Geri Yüklendi)";
                        _context.DbContext.WarehouseShelves.Update(existingShelf);
                        createdCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                
                 var lastResult = _context.LastSaveChangesResult;
                 if (!lastResult.IsOk)
                {
                    if (lastResult.Exception != null)
                         rs.AddError(lastResult.Exception.ToString());
                    else
                         rs.AddError("Kayıt sırasında hata oluştu.");
                }
                else
                {
                     if(skippedCount > 0)
                        rs.AddSuccess($"{createdCount} raf oluşturuldu/geri yüklendi. {skippedCount} raf zaten mevcut olduğu için atlandı.");
                     else
                        rs.AddSuccess($"{createdCount} raf başarıyla oluşturuldu.");
                }
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("BatchCreateShelves Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> BatchDeleteShelves(AuditWrapDto<WarehouseShelfBatchDeleteDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var ids = model.Dto.Ids;
                if (ids == null || !ids.Any())
                {
                    rs.AddError("Silinecek raf seçilmedi.");
                    return rs;
                }

                // Check dependencies (Product Stocks) for ANY of the shelves
                var stockExists = await _context.DbContext.ProductStocks.AsNoTracking()
                    .AnyAsync(x => ids.Contains(x.WarehouseShelfId) && x.Quantity > 0 && x.Status != (int)EntityStatus.Deleted);
                
                if (stockExists)
                {
                    rs.AddError("Seçilen raflardan bazılarında ürün stoğu var. Önce stokları boşaltmalısınız.");
                    return rs;
                }

                await _context.DbContext.WarehouseShelves
                    .Where(f => ids.Contains(f.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                                              .SetProperty(a => a.DeletedDate, DateTime.Now)
                                              .SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                rs.AddSuccess($"{ids.Count} raf silindi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("BatchDeleteShelves Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
