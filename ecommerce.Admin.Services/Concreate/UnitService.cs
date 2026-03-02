using AutoMapper;
using ecommerce.Admin.Domain.Dtos.UnitDto;
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
using Npgsql;

namespace ecommerce.Admin.Services.Concreate;

public class UnitService : IUnitService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<Unit> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IRadzenPagerService<UnitListDto> _radzenPagerService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITenantProvider _tenantProvider;

    public UnitService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
        IRadzenPagerService<UnitListDto> radzenPagerService,
        IServiceScopeFactory serviceScopeFactory,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _repository = context.GetRepository<Unit>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
        _serviceScopeFactory = serviceScopeFactory;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult<Paging<IQueryable<UnitListDto>>>> GetUnits(PageSetting pager)
    {
        IActionResult<Paging<IQueryable<UnitListDto>>> response = new() { Result = new() };
        try
        {
            var entities = await _repository.GetAllAsync(
                predicate: x => x.Status != (int)EntityStatus.Deleted);

            var mapped = _mapper.Map<List<UnitListDto>>(entities);

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
            _logger.LogError("GetUnits Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<List<UnitListDto>>> GetUnits()
    {
        var response = new IActionResult<List<UnitListDto>> { Result = new List<UnitListDto>() };
        try
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = ctx.GetRepository<Unit>();
                var items = await repo.GetAllAsync(predicate: x => x.Status != (int)EntityStatus.Deleted);
                var mapped = _mapper.Map<List<UnitListDto>>(items);
                response.Result = mapped;
            }
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetUnits Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<UnitUpsertDto>> GetUnitById(int id)
    {
        var response = new IActionResult<UnitUpsertDto> { Result = new() };
        try
        {
            var entity = await _repository.GetFirstOrDefaultAsync(predicate: x => x.Id == id && x.Status != (int)EntityStatus.Deleted);
            if (entity == null)
            {
                response.AddError("Birim bulunamadı");
                return response;
            }

            var mapped = _mapper.Map<UnitUpsertDto>(entity);
            response.Result = mapped;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetUnitById Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<Empty>> UpsertUnit(AuditWrapDto<UnitUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!model.Dto.Id.HasValue || model.Dto.Id == 0)
            {
                // Check for duplicate name globally
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(u => u.Name.ToLower() == model.Dto.Name.ToLower() && u.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{model.Dto.Name}' isimli birim zaten mevcut.");
                    return rs;
                }

                // Insert
                var entity = _mapper.Map<Unit>(model.Dto);
                entity.CreatedDate = DateTime.Now;
                entity.CreatedId = model.UserId;
                entity.Status = (int)EntityStatus.Active;
                entity.BranchId = null; // Forces units to be global
                await _repository.InsertAsync(entity);
            }
            else
            {
                // Update - ExecuteUpdateAsync kullanarak tracking sorununu önle
                var exists = await _repository.GetFirstOrDefaultAsync(
                    predicate: x => x.Id == model.Dto.Id && x.Status != (int)EntityStatus.Deleted,
                    disableTracking: true);

                if (exists == null)
                {
                    rs.AddError("Birim bulunamadı");
                    return rs;
                }

                // Check for duplicate name globally (excluding current entity)
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(u => u.Id != model.Dto.Id && u.Name.ToLower() == model.Dto.Name.ToLower() && u.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{model.Dto.Name}' isimli birim zaten mevcut.");
                    return rs;
                }

                await _context.DbContext.Units
                    .Where(x => x.Id == model.Dto.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.Name, model.Dto.Name)
                        .SetProperty(c => c.ModifiedId, model.UserId)
                        .SetProperty(c => c.ModifiedDate, DateTime.Now));
            }

            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Successfull");
                return rs;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null)
                {
                    if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        rs.AddError($"'{model.Dto.Name}' isimli birim zaten mevcut.");
                    }
                    else
                    {
                        rs.AddError(lastResult.Exception.ToString());
                    }
                }
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("UpsertUnit Exception {Ex}", ex.ToString());
            if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                rs.AddError($"'{model.Dto.Name}' isimli birim zaten mevcut.");
            }
            else
            {
                rs.AddSystemError(ex.ToString());
            }
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> DeleteUnit(AuditWrapDto<UnitDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            await _context.DbContext.Units
                .Where(f => f.Id == model.Dto.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                    .SetProperty(a => a.DeletedDate, DateTime.Now)
                    .SetProperty(a => a.DeletedId, model.UserId));

            await _context.SaveChangesAsync();

            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Birim silindi.");
                return rs;
            }

            if (lastResult.Exception != null)
                rs.AddError(lastResult.Exception.ToString());

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError($"DeleteUnit Exception: {ex.ToString()}");
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
}
