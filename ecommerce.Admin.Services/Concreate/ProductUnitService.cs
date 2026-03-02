using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductUnitDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Interfaces;

namespace ecommerce.Admin.Services.Concreate;

public class ProductUnitService : IProductUnitService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<ProductUnit> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IRadzenPagerService<ProductUnitListDto> _radzenPagerService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITenantProvider _tenantProvider;

    public ProductUnitService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
        IRadzenPagerService<ProductUnitListDto> radzenPagerService,
        IServiceScopeFactory serviceScopeFactory,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _repository = context.GetRepository<ProductUnit>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
        _serviceScopeFactory = serviceScopeFactory;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult<Paging<IQueryable<ProductUnitListDto>>>> GetProductUnits(PageSetting pager)
    {
        IActionResult<Paging<IQueryable<ProductUnitListDto>>> response = new() { Result = new() };
        try
        {
            var entities = await _repository.GetAllAsync(
                predicate: x => x.Status != (int)EntityStatus.Deleted,
                include: q => q
                    .Include(x => x.Product)
                    .Include(x => x.Unit));

            var mapped = entities
                .Select(x => new ProductUnitListDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = x.Product != null ? x.Product.Name : "",
                    UnitId = x.UnitId,
                    UnitName = x.Unit != null ? x.Unit.Name : "",
                    Barcode = x.Barcode,
                    UnitValue = x.UnitValue,
                    CreatedDate = x.CreatedDate
                })
                .ToList();

            if (mapped?.Count > 0)
            {
                response.Result.Data = mapped
                    .AsQueryable()
                    .OrderByDescending(x => x.Id);
            }

            response.Result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetProductUnits Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<List<ProductUnitListDto>>> GetProductUnitsByProductId(int productId)
    {
        var response = new IActionResult<List<ProductUnitListDto>> { Result = new List<ProductUnitListDto>() };
        try
        {
            var currentBranchId = _tenantProvider.GetCurrentBranchId();
            var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = ctx.GetRepository<ProductUnit>();
                var items = await repo.GetAllAsync(
                    predicate: x => x.ProductId == productId && x.Status != (int)EntityStatus.Deleted,
                    include: q => q
                        .Include(x => x.Product)
                        .Include(x => x.Unit),
                    ignoreQueryFilters: true);
                
                var mapped = items
                    .Select(x => new ProductUnitListDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.Product != null ? x.Product.Name : "",
                        UnitId = x.UnitId,
                        UnitName = x.Unit != null ? x.Unit.Name : "",
                        Barcode = x.Barcode,
                        UnitValue = x.UnitValue,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList();
                
                response.Result = mapped;
            }
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetProductUnitsByProductId Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<List<ProductUnitListDto>>> GetProductUnitsByProductIds(List<int> productIds)
    {
        var response = new IActionResult<List<ProductUnitListDto>> { Result = new List<ProductUnitListDto>() };
        try
        {
            if (productIds == null || !productIds.Any())
                return response;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = ctx.GetRepository<ProductUnit>();
                var items = await repo.GetAllAsync(
                    predicate: x => productIds.Contains(x.ProductId) && x.Status != (int)EntityStatus.Deleted,
                    include: q => q
                        .Include(x => x.Product)
                        .Include(x => x.Unit));

                var mapped = items
                    .Select(x => new ProductUnitListDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.Product != null ? x.Product.Name : "",
                        UnitId = x.UnitId,
                        UnitName = x.Unit != null ? x.Unit.Name : "",
                        Barcode = x.Barcode,
                        UnitValue = x.UnitValue,
                        CreatedDate = x.CreatedDate
                    })
                    .ToList();

                response.Result = mapped;
            }
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetProductUnitsByProductIds Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<ProductUnitUpsertDto>> GetProductUnitById(int id)
    {
        var response = new IActionResult<ProductUnitUpsertDto> { Result = new() };
        try
        {
            var entity = await _repository.GetFirstOrDefaultAsync(
                predicate: x => x.Id == id && x.Status != (int)EntityStatus.Deleted);
            
            if (entity == null)
            {
                response.AddError("Ürün birimi bulunamadı");
                return response;
            }

            var mapped = _mapper.Map<ProductUnitUpsertDto>(entity);
            response.Result = mapped;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetProductUnitById Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<Empty>> UpsertProductUnit(AuditWrapDto<ProductUnitUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repository = context.GetRepository<ProductUnit>();
                
                if (!model.Dto.Id.HasValue || model.Dto.Id == 0)
                {
                    // Insert
                    
                    // Check for existing assignment
                    var existing = await repository.GetFirstOrDefaultAsync(
                        predicate: x => x.ProductId == model.Dto.ProductId && x.UnitId == model.Dto.UnitId && x.Status != (int)EntityStatus.Deleted);
                    
                    if (existing != null)
                    {
                        rs.AddError("Bu birim bu ürün için zaten tanımlı.");
                        return rs;
                    }

                    var entity = _mapper.Map<ProductUnit>(model.Dto);
                    entity.CreatedDate = DateTime.Now;
                    entity.CreatedId = model.UserId;
                    entity.Status = (int)EntityStatus.Active;
                    
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    if (_tenantProvider.IsMultiTenantEnabled && currentBranchId > 0)
                    {
                        entity.BranchId = currentBranchId;
                    }
                    
                    await repository.InsertAsync(entity);
                }
                else
                {
                    // Update
                    var exists = await repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == model.Dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);

                    if (exists == null)
                    {
                        rs.AddError("Ürün birimi bulunamadı");
                        return rs;
                    }

                    await context.DbContext.ProductUnits
                        .Where(x => x.Id == model.Dto.Id)
                        .ExecuteUpdateAsync(x => x
                            .SetProperty(c => c.ProductId, model.Dto.ProductId)
                            .SetProperty(c => c.UnitId, model.Dto.UnitId)
                            .SetProperty(c => c.Barcode, model.Dto.Barcode)
                            .SetProperty(c => c.UnitValue, model.Dto.UnitValue)
                            .SetProperty(c => c.ModifiedId, model.UserId)
                            .SetProperty(c => c.ModifiedDate, DateTime.Now));
                }

                await context.SaveChangesAsync();

                var lastResult = context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Ürün birimi kaydedildi.");
                    return rs;
                }

                if (lastResult.Exception != null)
                    rs.AddError(lastResult.Exception.ToString());

                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("UpsertProductUnit Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> DeleteProductUnit(AuditWrapDto<ProductUnitDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            await _context.DbContext.ProductUnits
                .Where(f => f.Id == model.Dto.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                    .SetProperty(a => a.DeletedDate, DateTime.Now)
                    .SetProperty(a => a.DeletedId, model.UserId));

            await _context.SaveChangesAsync();

            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Ürün birimi silindi.");
                return rs;
            }

            if (lastResult.Exception != null)
                rs.AddError(lastResult.Exception.ToString());

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError($"DeleteProductUnit Exception: {ex.ToString()}");
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
}
