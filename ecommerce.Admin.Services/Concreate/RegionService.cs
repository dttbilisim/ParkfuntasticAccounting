using AutoMapper;
using ecommerce.Admin.Domain.Dtos.RegionDto;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ecommerce.Admin.Domain.Concreate
{
    public class RegionService : IRegionService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Region> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<RegionListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "regions";

        public RegionService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<RegionListDto> radzenPagerService, IServiceScopeFactory serviceScopeFactory, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<Region>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Empty>> DeleteRegion(AuditWrapDto<RegionDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                await _context.DbContext.Regions.Where(f => f.Id == model.Dto.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.DeletedId, model.UserId)
                        .SetProperty(x => x.DeletedDate, DateTime.Now)
                        .SetProperty(x => x.Status, EntityStatus.Deleted.GetHashCode()));

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteRegion Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<RegionListDto>>>> GetRegions(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<RegionListDto>>> response = new() { Result = new() };
            try
            {
                var regions = await _repository.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active);
                var mappedEntities = _mapper.Map<List<RegionListDto>>(regions);
                if (mappedEntities != null)
                {
                    if (mappedEntities.Count > 0)
                    {
                        response.Result.Data = mappedEntities.AsQueryable();
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
                _logger.LogError("GetRegions Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<RegionListDto>>> GetRegions()
        {
            var response = new IActionResult<List<RegionListDto>>
            {
                Result = new List<RegionListDto>()
            };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<Region>();
                    
                    var regions = await repository.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active, disableTracking: true);
                    var mappedEntities = _mapper.Map<List<RegionListDto>>(regions);
                    if (mappedEntities != null)
                    {
                        if (mappedEntities.Count > 0)
                            response.Result = mappedEntities;
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetRegions Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<RegionUpsertDto>> GetRegionById(int regionId)
        {
            var response = new IActionResult<RegionUpsertDto>
            {
                Result = new()
            };
            try
            {
                var region = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == regionId);
                var mappedEntity = _mapper.Map<RegionUpsertDto>(region);
                if (mappedEntity != null)
                {
                    response.Result = mappedEntity;
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetRegionById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertRegion(AuditWrapDto<RegionUpsertDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    // Check for duplicate name globally
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(r => r.Name.ToLower() == dto.Name.ToLower() && r.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli bölge zaten mevcut.");
                        return response;
                    }

                    var entity = _mapper.Map<Region>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true,
                        ignoreQueryFilters: true);
                    if (current == null)
                    {
                        response.AddError("Bölge bulunamadı");
                        return response;
                    }

                    // Check for duplicate name globally (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(r => r.Id != dto.Id && r.Name.ToLower() == dto.Name.ToLower() && r.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli bölge zaten mevcut.");
                        return response;
                    }

                    var updated = _mapper.Map<Region>(dto);
                    updated.Id = current.Id;
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                }
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                    {
                        if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            response.AddError($"'{dto.Name}' isimli bölge zaten mevcut.");
                        }
                        else
                        {
                            response.AddError(lastResult.Exception.ToString());
                        }
                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertRegion Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    response.AddError($"'{model.Dto.Name}' isimli bölge zaten mevcut.");
                }
                else
                {
                    response.AddSystemError(ex.ToString());
                }
                return response;
            }
        }
    }
}

