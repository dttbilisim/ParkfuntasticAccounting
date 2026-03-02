using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ScaleUnitDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
namespace ecommerce.Admin.Domain.Concreate
{
    public class ScaleUnitService : IScaleUnitService
    {
        private const string MENU_NAME = "scaleunits";
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<ScaleUnit> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<ScaleUnitListDto> _radzenPagerService;

        public ScaleUnitService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<ScaleUnitListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<ScaleUnit>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }


        public async Task<IActionResult<Empty>> DeleteScaleUnit(AuditWrapDto<ScaleUnitDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                await _context.DbContext.ScaleUnits.Where(f => f.Id == model.Dto.Id).
                    ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).
                    SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

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
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteScaleUnit Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<ScaleUnitUpsertDto>> GetScaleUnitById(int Id)
        {
            var rs = new IActionResult<ScaleUnitUpsertDto>
            {
                Result = new()
            };
            try
            {
                var entity = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<ScaleUnitUpsertDto>(entity);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetScaleUnitById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<ScaleUnitListDto>>>> GetScaleUnits(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<ScaleUnitListDto>>> response = new() { Result = new() };
            try
            {

                var entities = await _repository.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active);
                var mappedList = _mapper.Map<List<ScaleUnitListDto>>(entities);
                if (mappedList != null)
                {
                    if (mappedList.Count > 0)
                    {
                        response.Result.Data = mappedList.AsQueryable();
                    }
                }

                if (response.Result.Data != null)
                    response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);


                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);


                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetScaleUnits Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<ScaleUnitListDto>>> GetScaleUnits()
        {
            var rs = new IActionResult<List<ScaleUnitListDto>>
            {
                Result = new()
            };
            try
            {
                var entities = _repository.GetAll(predicate: x => x.Status == (int)EntityStatus.Active);
                var mapped = _mapper.Map<List<ScaleUnitListDto>>(entities);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;

                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetScaleUnits Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertScaleUnit(AuditWrapDto<ScaleUnitUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    // Check for duplicate name globally
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(su => su.Name.ToLower() == dto.Name.ToLower() && su.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli tartı birimi zaten mevcut.");
                        return rs;
                    }

                    var entity = _mapper.Map<ScaleUnit>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if (current == null)
                    {
                        rs.AddError("Tartı birimi bulunamadı");
                        return rs;
                    }

                    // Check for duplicate name globally (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(su => su.Id != dto.Id && su.Name.ToLower() == dto.Name.ToLower() && su.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli tartı birimi zaten mevcut.");
                        return rs;
                    }

                    await _context.DbContext.ScaleUnits.Where(x => x.Id == model.Dto.Id)
                        .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.Name, model.Dto.Name)
                        .SetProperty(c => c.Status, dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive)
                        .SetProperty(c => c.ModifiedId, model.UserId)
                        .SetProperty(c => c.ModifiedDate, DateTime.Now));
                }
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
                            rs.AddError($"'{dto.Name}' isimli birim zaten mevcut.");
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
                _logger.LogError("UpsertScaleUnits Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    rs.AddError($"'{model.Dto.Name}' isimli tartı birimi zaten mevcut.");
                }
                else
                {
                    rs.AddSystemError(ex.ToString());
                }
                return rs;
            }
        }
    }
}
