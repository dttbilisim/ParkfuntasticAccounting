using AutoMapper;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Interfaces;
using ecommerce.EFCore.UnitOfWork;
using ecommerce.Core.Entities.Hierarchical;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ecommerce.Core.Identity;
using Npgsql;

namespace ecommerce.Admin.Services.Concreate
{
    public class CorporationService : ICorporationService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<CorporationService> _logger;
        private readonly IRadzenPagerService<CorporationListDto> _radzenPagerService;
        private readonly CurrentUser _currentUser;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "corporations";
        

        public CorporationService(
            IServiceScopeFactory serviceScopeFactory,
            IMapper mapper,
            ILogger<CorporationService> logger,
            IRadzenPagerService<CorporationListDto> radzenPagerService,
            CurrentUser currentUser,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _currentUser = currentUser;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Paging<IQueryable<CorporationListDto>>>> GetCorporations(PageSetting pager)
        {
            var rs = new IActionResult<Paging<IQueryable<CorporationListDto>>> { Result = new() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Transfer user context to the new scope
                var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
                scopedCurrentUser.SetUser(_currentUser.Principal);

                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var scopedRepo = scopedUow.GetRepository<Corporation>();

                var isGlobalAdmin = tenantProvider.IsGlobalAdmin;
                var user = _currentUser.Principal;

                var query = scopedRepo.GetAll(predicate: x => x.Status != (int)EntityStatus.Deleted);

                if (!isGlobalAdmin && user != null)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        var allowedBranchIds = scopedUow.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();

                        var allowedCorpIds = scopedUow.DbContext.Branches
                            .AsNoTracking()
                            .Where(b => allowedBranchIds.Contains(b.Id))
                            .Select(b => b.CorporationId)
                            .Distinct()
                            .ToList();

                        query = query.Where(x => allowedCorpIds.Contains(x.Id));
                    }
                }

                var entities = await query.ToListAsync();
                var mappedList = _mapper.Map<List<CorporationListDto>>(entities);
                
                rs.Result.Data = mappedList.AsQueryable();
                rs.Result = _radzenPagerService.MakeDataQueryable(rs.Result.Data, pager);
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCorporations Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<List<CorporationListDto>>> GetAllActiveCorporations()
        {
            var rs = new IActionResult<List<CorporationListDto>> { Result = new() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Transfer user context to the new scope
                var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
                scopedCurrentUser.SetUser(_currentUser.Principal);

                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var scopedRepo = scopedUow.GetRepository<Corporation>();

                var isGlobalAdmin = tenantProvider.IsGlobalAdmin;
                var user = _currentUser.Principal;

                var query = scopedRepo.GetAll(predicate: x => x.Status == (int)EntityStatus.Active && x.IsActive);

                if (!isGlobalAdmin && user != null)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        var allowedBranchIds = scopedUow.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();

                        var allowedCorpIds = scopedUow.DbContext.Branches
                            .AsNoTracking()
                            .Where(b => allowedBranchIds.Contains(b.Id))
                            .Select(b => b.CorporationId)
                            .Distinct()
                            .ToList();

                        query = query.Where(x => allowedCorpIds.Contains(x.Id));
                    }
                }

                var entities = await query.ToListAsync();
                rs.Result = _mapper.Map<List<CorporationListDto>>(entities);
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllActiveCorporations Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<CorporationUpsertDto>> GetCorporationById(int id)
        {
            var rs = new IActionResult<CorporationUpsertDto>();
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<Corporation>();

                var entity = await scopedRepo.FindAsync(id);
                if (entity != null)
                {
                    rs.Result = _mapper.Map<CorporationUpsertDto>(entity);
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCorporationById Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertCorporation(AuditWrapDto<CorporationUpsertDto> model)
        {
            var rs = new IActionResult<Empty>();
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<Corporation>();

                if (model.Dto.Id.HasValue && model.Dto.Id.Value > 0)
                {
                    var entity = await scopedRepo.FindAsync(model.Dto.Id.Value);
                    if (entity == null)
                    {
                        rs.AddError("Kayıt bulunamadı");
                        return rs;
                    }

                    // Check for duplicate name globally (excluding current entity)
                    var duplicate = await scopedRepo.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(c => c.Id != model.Dto.Id && c.Name.ToLower() == model.Dto.Name.ToLower() && c.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{model.Dto.Name}' isimli kurum zaten mevcut.");
                        return rs;
                    }

                    _mapper.Map(model.Dto, entity);
                    entity.ModifiedDate = DateTime.Now;
                    entity.ModifiedId = model.UserId;
                    scopedRepo.Update(entity);
                }
                else
                {
                    // Check for duplicate name globally
                    var duplicate = await scopedRepo.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(c => c.Name.ToLower() == model.Dto.Name.ToLower() && c.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{model.Dto.Name}' isimli kurum zaten mevcut.");
                        return rs;
                    }

                    var entity = _mapper.Map<Corporation>(model.Dto);
                    entity.CreatedDate = DateTime.Now;
                    entity.CreatedId = model.UserId;
                    entity.Status = (int)EntityStatus.Active;
                    await scopedRepo.InsertAsync(entity);
                }

                await scopedUow.SaveChangesAsync();
                var lastResult = scopedUow.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                    {
                        if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            rs.AddError($"'{model.Dto.Name}' isimli kurum zaten mevcut.");
                        }
                        else
                        {
                            rs.AddError("Bir hata oluştu. Lütfen tekrar deneyiniz.");
                            _logger.LogError(lastResult.Exception, "UpsertCorporation save error");
                        }
                    }
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertCorporation Error: {Ex}", ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    rs.AddError($"'{model.Dto.Name}' isimli kurum zaten mevcut.");
                }
                else
                {
                    rs.AddError(ex.Message);
                }
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteCorporation(int id, int userId)
        {
            var rs = new IActionResult<Empty>();
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<Corporation>();

                var entity = await scopedRepo.FindAsync(id);
                if (entity != null)
                {
                    entity.Status = (int)EntityStatus.Deleted;
                    entity.DeletedDate = DateTime.Now;
                    entity.DeletedId = userId;
                    scopedRepo.Update(entity);
                    await scopedUow.SaveChangesAsync();
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteCorporation Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }
    }
}
