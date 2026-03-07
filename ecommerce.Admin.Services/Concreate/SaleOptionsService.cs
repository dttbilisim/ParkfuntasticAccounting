using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SaleOptionsDto;
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
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ecommerce.Admin.Services.Concreate;

public class SaleOptionsService : ISaleOptionsService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<SaleOptions> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<SaleOptionsService> _logger;
    private readonly IRadzenPagerService<SaleOptionsListDto> _radzenPagerService;

    public SaleOptionsService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger<SaleOptionsService> logger,
        IRadzenPagerService<SaleOptionsListDto> radzenPagerService)
    {
        _context = context;
        _repository = context.GetRepository<SaleOptions>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
    }

    public async Task<IActionResult<Paging<IQueryable<SaleOptionsListDto>>>> GetSaleOptions(PageSetting pager)
    {
        var response = new IActionResult<Paging<IQueryable<SaleOptionsListDto>>> { Result = new() };
        try
        {
            var entities = await _repository.GetAllAsync(
                predicate: x => x.Status != (int)EntityStatus.Deleted);

            var mapped = _mapper.Map<List<SaleOptionsListDto>>(entities) ?? new List<SaleOptionsListDto>();
            response.Result.Data = mapped
                .AsQueryable()
                .OrderByDescending(x => x.Id);

            response.Result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSaleOptions error");
            response.AddSystemError(ex.Message);
            return response;
        }
    }

    public async Task<IActionResult<SaleOptionsUpsertDto>> GetSaleOptionsById(int id)
    {
        var response = new IActionResult<SaleOptionsUpsertDto> { Result = new() };
        try
        {
            var entity = await _repository.GetFirstOrDefaultAsync(
                predicate: x => x.Id == id && x.Status != (int)EntityStatus.Deleted);

            if (entity == null)
            {
                response.AddError("Satış seçeneği bulunamadı");
                return response;
            }

            response.Result = _mapper.Map<SaleOptionsUpsertDto>(entity);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSaleOptionsById error");
            response.AddSystemError(ex.Message);
            return response;
        }
    }

    public async Task<IActionResult<Empty>> UpsertSaleOptions(AuditWrapDto<SaleOptionsUpsertDto> model)
    {
        var response = new IActionResult<Empty> { Result = new() };
        try
        {
            if (!model.Dto.Id.HasValue || model.Dto.Id == 0)
            {
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(x => x.Name.ToLower() == model.Dto.Name.ToLower() && x.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    response.AddError($"'{model.Dto.Name}' isimli satış seçeneği zaten mevcut.");
                    return response;
                }

                var entity = _mapper.Map<SaleOptions>(model.Dto);
                entity.CreatedDate = DateTime.Now;
                entity.CreatedId = model.UserId;
                entity.Status = (int)EntityStatus.Active;
                await _repository.InsertAsync(entity);
            }
            else
            {
                var exists = await _repository.GetFirstOrDefaultAsync(
                    predicate: x => x.Id == model.Dto.Id && x.Status != (int)EntityStatus.Deleted);

                if (exists == null)
                {
                    response.AddError("Satış seçeneği bulunamadı");
                    return response;
                }

                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(x => x.Id != model.Dto.Id && x.Name.ToLower() == model.Dto.Name.ToLower() && x.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    response.AddError($"'{model.Dto.Name}' isimli satış seçeneği zaten mevcut.");
                    return response;
                }

                _mapper.Map(model.Dto, exists);
                exists.ModifiedId = model.UserId;
                exists.ModifiedDate = DateTime.Now;
                _repository.Update(exists);
            }

            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                response.AddSuccess("Başarılı");
                return response;
            }

            if (lastResult?.Exception != null)
            {
                if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    response.AddError($"'{model.Dto.Name}' isimli satış seçeneği zaten mevcut.");
                else
                    response.AddError("Bir hata oluştu.");
            }
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpsertSaleOptions error");
            if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                response.AddError($"'{model.Dto.Name}' isimli satış seçeneği zaten mevcut.");
            else
                response.AddSystemError(ex.Message);
            return response;
        }
    }

    public async Task<IActionResult<Empty>> DeleteSaleOptions(AuditWrapDto<SaleOptionsDeleteDto> model)
    {
        var response = new IActionResult<Empty> { Result = new() };
        try
        {
            await _context.DbContext.SaleOptions
                .Where(x => x.Id == model.Dto.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                    .SetProperty(a => a.DeletedDate, DateTime.Now)
                    .SetProperty(a => a.DeletedId, model.UserId));

            await _context.SaveChangesAsync();

            if (_context.LastSaveChangesResult.IsOk)
            {
                response.AddSuccess("Satış seçeneği silindi.");
                return response;
            }

            if (_context.LastSaveChangesResult.Exception != null)
                response.AddError(_context.LastSaveChangesResult.Exception.ToString());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteSaleOptions error");
            response.AddSystemError(ex.Message);
            return response;
        }
    }
}
