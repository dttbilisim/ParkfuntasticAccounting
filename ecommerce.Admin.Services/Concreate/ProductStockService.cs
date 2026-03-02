using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductStockDto;
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
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Admin.Domain.Concreate
{
    public class ProductStockService : IProductStockService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<ProductStock> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductStockService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "stock-transfer";

        public ProductStockService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger<ProductStockService> logger, IServiceScopeFactory serviceScopeFactory, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<ProductStock>();
            _mapper = mapper;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Paging<List<ProductStockListDto>>>> GetStocks(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<ProductStockListDto>>>();

            try
            {
                response.Result = await _repository.GetAll(true)
                    .Include(s => s.Product)
                    .Include(s => s.Shelf).ThenInclude(s => s.Warehouse)
                    .Where(s => s.Status != (int)EntityStatus.Deleted)
                    .ToPagedResultAsync<ProductStockListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetStocks Exception " + e);
                response.AddSystemError(e.Message);
            }

            return response;
        }

        public async Task<IActionResult<List<ProductStockListDto>>> GetStocksByProduct(int productId)
        {
             var response = new IActionResult<List<ProductStockListDto>> { Result = new() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<ProductStock>();
                    
                    var stocks = await repository.GetAllAsync(
                        predicate: f => f.ProductId == productId && f.Status == (int)EntityStatus.Active, 
                        include: i => i.Include(x => x.Shelf).ThenInclude(x => x.Warehouse),
                        disableTracking: true);
                    
                    response.Result = _mapper.Map<List<ProductStockListDto>>(stocks);
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetStocksByProduct Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        
        public async Task<IActionResult<List<ProductStockListDto>>> GetStocksByShelf(int shelfId)
        {
             var response = new IActionResult<List<ProductStockListDto>> { Result = new() };
            try
            {
                var stocks = await _repository.GetAllAsync(
                    predicate: f => f.WarehouseShelfId == shelfId && f.Status == (int)EntityStatus.Active, 
                    include: i => i.Include(x => x.Product),
                    disableTracking: true);
                
                response.Result = _mapper.Map<List<ProductStockListDto>>(stocks);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetStocksByShelf Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertStock(AuditWrapDto<ProductStockUpsertDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                
                if (!dto.Id.HasValue || dto.Id == 0)
                {
                    // Check if unique index violation might occur (ProductId + ShelfId)
                    var existing = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.ProductId == dto.ProductId && x.WarehouseShelfId == dto.WarehouseShelfId && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    
                    if (existing != null)
                    {
                        rs.AddError("Bu ürün bu rafta zaten mevcut. Miktarı güncelleyiniz.");
                        return rs;
                    }

                    var entity = _mapper.Map<ProductStock>(dto);
                    entity.Status = (int)EntityStatus.Active; 
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                    rs.AddSuccess("Stok eklendi.");
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if (current == null)
                    {
                        rs.AddError("Stok kaydı bulunamadı");
                        return rs;
                    }
                    var updated = _mapper.Map<ProductStock>(dto);
                    updated.Id = current.Id;
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = (int)EntityStatus.Active;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                    rs.AddSuccess("Stok güncellendi.");
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
                _logger.LogError("UpsertStock Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteStock(AuditWrapDto<ProductStockDeleteDto> model)
        {
             var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                await _context.DbContext.ProductStocks.Where(f => f.Id == model.Dto.Id).
                    ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).
                    SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                rs.AddSuccess("Stok silindi");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteStock Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<ProductStockUpsertDto>> GetStockById(int Id)
        {
             var rs = new IActionResult<ProductStockUpsertDto> { Result = new ProductStockUpsertDto() };
            try
            {
                var entity = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var dto = _mapper.Map<ProductStockUpsertDto>(entity);
                if (dto != null)
                {
                     dto.StatusBool = dto.Status == (int)EntityStatus.Active;
                    rs.Result = dto;
                }
                else rs.AddError("Stok Kaydı Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetStockById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> TransferStock(AuditWrapDto<ProductStockTransferDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                
                // 1. Get Source Stock
                var sourceStock = await _context.DbContext.ProductStocks
                    .Include(x => x.Shelf)
                    .FirstOrDefaultAsync(x => x.WarehouseShelfId == dto.SourceShelfId && x.ProductId == dto.ProductId && x.Status != (int)EntityStatus.Deleted);

                if (sourceStock == null)
                {
                    rs.AddError("Kaynak rafta bu ürün bulunamadı.");
                    return rs;
                }

                if (sourceStock.Quantity < dto.Quantity)
                {
                    rs.AddError($"Yetersiz stok. Mevcut: {sourceStock.Quantity}, İstenen: {dto.Quantity}");
                    return rs;
                }

                // 2. Get/Create Target Stock
                var targetStock = await _context.DbContext.ProductStocks
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.WarehouseShelfId == dto.TargetShelfId && x.ProductId == dto.ProductId);

                if (targetStock == null)
                {
                    targetStock = new ProductStock
                    {
                        ProductId = dto.ProductId,
                        WarehouseShelfId = dto.TargetShelfId,
                        Quantity = 0,
                        Status = (int)EntityStatus.Active,
                        CreatedId = model.UserId,
                        CreatedDate = DateTime.Now
                    };
                    await _context.DbContext.ProductStocks.AddAsync(targetStock);
                }
                else if (targetStock.Status == (int)EntityStatus.Deleted)
                {
                     // Reactivate
                     targetStock.Status = (int)EntityStatus.Active;
                     targetStock.ModifiedId = model.UserId;
                     targetStock.ModifiedDate = DateTime.Now;
                     _context.DbContext.ProductStocks.Update(targetStock);
                }

                // 3. Move Quantity
                sourceStock.Quantity -= dto.Quantity;
                sourceStock.ModifiedId = model.UserId;
                sourceStock.ModifiedDate = DateTime.Now;
                
                targetStock.Quantity += dto.Quantity;
                if(targetStock.Id > 0) // If not new
                {
                    targetStock.ModifiedId = model.UserId;
                    targetStock.ModifiedDate = DateTime.Now;
                }



            _context.DbContext.ProductStocks.Update(sourceStock);
            if (targetStock.Id > 0) _context.DbContext.ProductStocks.Update(targetStock); // If new, AddAsync covers it

            await _context.SaveChangesAsync();
            
            var lastResult = _context.LastSaveChangesResult;
             if (!lastResult.IsOk)
            {
                if (lastResult.Exception != null)
                     rs.AddError(lastResult.Exception.ToString());
                else
                     rs.AddError("Transfer sırasında hata oluştu.");
            }
            else
            {
         
                var transferLog = new StockTransferLog
                {
                    ProductId = dto.ProductId,
                    SourceWarehouseId = sourceStock.Shelf.WarehouseId,
                    TargetWarehouseId = (await _context.DbContext.WarehouseShelves.FindAsync(dto.TargetShelfId))?.WarehouseId ?? 0,
                    SourceShelfId = dto.SourceShelfId,
                    TargetShelfId = dto.TargetShelfId,
                    Quantity = dto.Quantity,
                    TransferredByUserId = model.UserId,
                    TransferDate = DateTime.Now,
                    BatchId = dto.BatchId,
                    Status = (int)EntityStatus.Active,
                    CreatedId = model.UserId,
                    CreatedDate = DateTime.Now
                };
                await _context.DbContext.StockTransferLogs.AddAsync(transferLog);
                await _context.SaveChangesAsync();
                
                rs.AddSuccess("Stok transferi başarılı.");
            }

            return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("TransferStock Exception " + ex.ToString());
                 rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<List<StockTransferLogListDto>>>> GetTransferLogs(PageSetting pager)
        {
            var rs = new IActionResult<Paging<List<StockTransferLogListDto>>> { Result = new Paging<List<StockTransferLogListDto>>() };
            try
            {
                var query = _context.DbContext.StockTransferLogs
                    .Include(x => x.Product)
                    .Include(x => x.SourceWarehouse)
                    .Include(x => x.TargetWarehouse)
                    .Include(x => x.SourceShelf)
                    .Include(x => x.TargetShelf)
                    .Where(x => x.Status != (int)EntityStatus.Deleted)
                    .OrderByDescending(x => x.TransferDate)
                    .AsQueryable();

                var totalCount = await query.CountAsync();

                var logs = await query
                    .Skip(pager.Skip ?? 0)
                    .Take(pager.Take ?? 10)
                    .Select(x => new StockTransferLogListDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        TransferDate = x.TransferDate,
                        ProductName = x.Product.Name,
                        SourceWarehouseId = x.SourceWarehouseId,
                        SourceWarehouseName = x.SourceWarehouse.Name,
                        TargetWarehouseId = x.TargetWarehouseId,
                        TargetWarehouseName = x.TargetWarehouse.Name,
                        SourceShelfId = x.SourceShelfId,
                        SourceShelfCode = x.SourceShelf.Code,
                        TargetShelfId = x.TargetShelfId,
                        TargetShelfCode = x.TargetShelf.Code,
                        Quantity = x.Quantity,
                        TransferredByUserName = ""
                    })
                    .ToListAsync();

                rs.Result.Data = logs;
                rs.Result.DataCount = totalCount;
                rs.AddSuccess("Transfer kayıtları listelendi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTransferLogs Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<List<StockTransferBatchDto>>>> GetTransferBatches(PageSetting pager)
        {
            var rs = new IActionResult<Paging<List<StockTransferBatchDto>>> { Result = new Paging<List<StockTransferBatchDto>>() };
            try
            {
                var query = _context.DbContext.StockTransferLogs
                    .Include(x => x.Product)
                    .Include(x => x.SourceWarehouse)
                    .Include(x => x.TargetWarehouse)
                    .Include(x => x.SourceShelf)
                    .Include(x => x.TargetShelf)
                    .Where(x => x.Status != (int)EntityStatus.Deleted)
                    .OrderByDescending(x => x.TransferDate)
                    .AsQueryable();

                // Group by BatchId
                var grouped = await query
                    .GroupBy(x => x.BatchId)
                    .Select(g => new
                    {
                        BatchId = g.Key,
                        TransferDate = g.Min(x => x.TransferDate),
                        SourceWarehouseName = g.First().SourceWarehouse.Name,
                        TargetWarehouseName = g.First().TargetWarehouse.Name,
                        SourceShelfCodes = string.Join(", ", g.Select(x => x.SourceShelf.Code).Distinct()),
                        TargetShelfCodes = string.Join(", ", g.Select(x => x.TargetShelf.Code).Distinct()),
                        TotalItems = g.Count(),
                        TotalQuantity = g.Sum(x => x.Quantity),
                        Items = g.Select(x => new StockTransferLogListDto
                        {
                            Id = x.Id,
                            ProductId = x.ProductId,
                            TransferDate = x.TransferDate,
                            ProductName = x.Product.Name,
                            SourceWarehouseId = x.SourceWarehouseId,
                            SourceWarehouseName = x.SourceWarehouse.Name,
                            TargetWarehouseId = x.TargetWarehouseId,
                            TargetWarehouseName = x.TargetWarehouse.Name,
                            SourceShelfId = x.SourceShelfId,
                            SourceShelfCode = x.SourceShelf.Code,
                            TargetShelfId = x.TargetShelfId,
                            TargetShelfCode = x.TargetShelf.Code,
                            Quantity = x.Quantity,
                            TransferredByUserName = ""
                        }).ToList()
                    })
                    .ToListAsync();

                var totalCount = grouped.Count;

                var batches = grouped
                    .Skip(pager.Skip ?? 0)
                    .Take(pager.Take ?? 10)
                    .Select(x => new StockTransferBatchDto
                    {
                        BatchId = x.BatchId,
                        TransferDate = x.TransferDate,
                        SourceWarehouseName = x.SourceWarehouseName,
                        TargetWarehouseName = x.TargetWarehouseName,
                        SourceShelfCodes = x.SourceShelfCodes,
                        TargetShelfCodes = x.TargetShelfCodes,
                        TotalItems = x.TotalItems,
                        TotalQuantity = x.TotalQuantity,
                        TransferredByUserName = "",
                        Items = x.Items
                    })
                    .ToList();

                rs.Result.Data = batches;
                rs.Result.DataCount = totalCount;
                rs.AddSuccess("Transfer batch kayıtları listelendi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTransferBatches Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<StockTransferLogListDto>>> GetTransferLogsByBatchId(Guid batchId)
        {
            var rs = new IActionResult<List<StockTransferLogListDto>> { Result = new List<StockTransferLogListDto>() };
            try
            {
                var logs = await _context.DbContext.StockTransferLogs
                    .Include(x => x.Product)
                    .Include(x => x.SourceWarehouse)
                    .Include(x => x.TargetWarehouse)
                    .Include(x => x.SourceShelf)
                    .Include(x => x.TargetShelf)
                    .Where(x => x.BatchId == batchId && x.Status != (int)EntityStatus.Deleted)
                    .Select(x => new StockTransferLogListDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        TransferDate = x.TransferDate,
                        ProductName = x.Product.Name,
                        SourceWarehouseId = x.SourceWarehouseId,
                        SourceWarehouseName = x.SourceWarehouse.Name,
                        TargetWarehouseId = x.TargetWarehouseId,
                        TargetWarehouseName = x.TargetWarehouse.Name,
                        SourceShelfId = x.SourceShelfId,
                        SourceShelfCode = x.SourceShelf.Code,
                        TargetShelfId = x.TargetShelfId,
                        TargetShelfCode = x.TargetShelf.Code,
                        Quantity = x.Quantity,
                        TransferredByUserName = ""
                    })
                    .ToListAsync();

                rs.Result = logs;
                rs.AddSuccess("Transfer kayıtları getirildi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTransferLogsByBatchId Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> UpdateTransferLogQuantity(int logId, decimal newQuantity, int userId)
        {
            var rs = new IActionResult<Empty>();
            try
            {
                var log = await _context.DbContext.StockTransferLogs
                    .Include(x => x.SourceShelf)
                    .Include(x => x.TargetShelf)
                    .FirstOrDefaultAsync(x => x.Id == logId);

                if (log == null)
                {
                    _logger.LogError($"UpdateTransferLogQuantity: Log not found for ID {logId}");
                    rs.AddError("Transfer kaydı bulunamadı.");
                    return rs;
                }

                if (newQuantity <= 0)
                {
                    rs.AddError("Miktar 0'dan büyük olmalıdır.");
                    return rs;
                }

                decimal oldQuantity = log.Quantity;
                decimal diff = newQuantity - oldQuantity;
                
                _logger.LogInformation($"UpdateTransferLogQuantity: LogId={logId}, OldQty={oldQuantity}, NewQty={newQuantity}, Diff={diff}");

                if (diff == 0)
                {
                    rs.AddSuccess("Değişiklik yok.");
                    return rs;
                }

                // Update Source Stock
                var sourceStock = await _context.DbContext.ProductStocks
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.WarehouseShelfId == log.SourceShelfId && x.ProductId == log.ProductId);
                
                if (sourceStock == null)
                {
                    _logger.LogError($"UpdateTransferLogQuantity: Source stock not found. ShelfId={log.SourceShelfId}, ProductId={log.ProductId}");
                    rs.AddError("Kaynak stok bulunamadı.");
                    return rs;
                }

                // If taking more from source (diff > 0), check availability
                if (diff > 0 && sourceStock.Quantity < diff)
                {
                    rs.AddError($"Kaynak rafta yeterli stok yok. Mevcut: {sourceStock.Quantity}, İstenen Ek: {diff}");
                    return rs;
                }

                sourceStock.Quantity -= diff;
                if (sourceStock.Status == (int)EntityStatus.Deleted) sourceStock.Status = (int)EntityStatus.Active;
                
                // Update Target Stock
                var targetStock = await _context.DbContext.ProductStocks
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.WarehouseShelfId == log.TargetShelfId && x.ProductId == log.ProductId);

                if (targetStock == null)
                {
                    // If target stock record doesn't exist (maybe deleted?), create or error. 
                    // Transfer logic usually ensures it exists.
                     rs.AddError("Hedef stok kaydı bulunamadı.");
                     return rs;
                }
                
                targetStock.Quantity += diff;
                if (targetStock.Status == (int)EntityStatus.Deleted) targetStock.Status = (int)EntityStatus.Active;
                // Update Log
                log.Quantity = newQuantity;
                // Update audit info if needed, e.g. UpdatedBy
                
                await _context.DbContext.SaveChangesAsync();
                
                rs.AddSuccess("Transfer miktarı güncellendi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateTransferLogQuantity Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
