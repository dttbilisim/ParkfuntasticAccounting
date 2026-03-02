using AutoMapper;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
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

namespace ecommerce.Admin.Services.Concreate
{
    public class BranchService : IBranchService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<BranchService> _logger;
        private readonly IRadzenPagerService<BranchListDto> _radzenPagerService;
        private readonly CurrentUser _currentUser;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "branches";

        public BranchService(
            IServiceScopeFactory serviceScopeFactory,
            IMapper mapper,
            ILogger<BranchService> logger,
            IRadzenPagerService<BranchListDto> radzenPagerService,
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

        public async Task<IActionResult<Paging<IQueryable<BranchListDto>>>> GetBranches(PageSetting pager)
        {
            var rs = new IActionResult<Paging<IQueryable<BranchListDto>>> { Result = new() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Transfer user context to the new scope
                var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
                scopedCurrentUser.SetUser(_currentUser.Principal);

                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var scopedRepo = scopedUow.GetRepository<Branch>();

                var isGlobalAdmin = tenantProvider.IsGlobalAdmin;
                var user = _currentUser.Principal;

                var query = scopedRepo.GetAll(
                    predicate: x => x.Status != (int)EntityStatus.Deleted,
                    include: i => i.Include(x => x.Corporation));

                if (!isGlobalAdmin && user != null)
                {
                    var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        var allowedBranchIds = scopedUow.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();

                        query = query.Where(x => allowedBranchIds.Contains(x.Id));
                    }
                }

                var entities = await query.ToListAsync();
                
                // Get Cities and Towns for name mapping
                var db = scopedUow.DbContext;

                var mappedList = entities.Select(x => new BranchListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    CorporationId = x.CorporationId,
                    CorporationName = x.Corporation.Name,
                    CityId = x.CityId,
                    CityName = x.CityId.HasValue ? db.City.FirstOrDefault(c => c.Id == x.CityId.Value)?.Name : null,
                    TownId = x.TownId,
                    TownName = x.TownId.HasValue ? db.Town.FirstOrDefault(t => t.Id == x.TownId.Value)?.Name : null,
                    IsActive = x.IsActive,
                    CreatedDate = x.CreatedDate
                }).ToList();

                rs.Result.Data = mappedList.AsQueryable();
                rs.Result = _radzenPagerService.MakeDataQueryable(rs.Result.Data, pager);
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBranches Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<List<BranchListDto>>> GetBranchesByCorporationId(int corporationId)
        {
            var rs = new IActionResult<List<BranchListDto>> { Result = new() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<Branch>();

                var entities = await scopedRepo.GetAllAsync(predicate: x => x.CorporationId == corporationId && x.Status == (int)EntityStatus.Active);
                rs.Result = _mapper.Map<List<BranchListDto>>(entities);
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBranchesByCorporationId Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<BranchUpsertDto>> GetBranchById(int id)
        {
            var rs = new IActionResult<BranchUpsertDto>();
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<Branch>();

                var entity = await scopedRepo.FindAsync(id);
                if (entity != null)
                {
                    rs.Result = _mapper.Map<BranchUpsertDto>(entity);
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBranchById Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<List<BranchListDto>>> GetAllActiveBranches()
        {
            var rs = new IActionResult<List<BranchListDto>> { Result = new() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Transfer user context to the new scope
                var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
                scopedCurrentUser.SetUser(_currentUser.Principal);

                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var scopedRepo = scopedUow.GetRepository<Branch>();

                var isGlobalAdmin = tenantProvider.IsGlobalAdmin;
                var user = _currentUser.Principal;

                var query = scopedRepo.GetAll(predicate: x => x.Status == (int)EntityStatus.Active);

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

                        query = query.Where(x => allowedBranchIds.Contains(x.Id));
                    }
                }

                var entities = await query.ToListAsync();
                rs.Result = _mapper.Map<List<BranchListDto>>(entities);
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllActiveBranches Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertBranch(AuditWrapDto<BranchUpsertDto> model)
        {
            var rs = new IActionResult<Empty>();
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<Branch>();

                if (model.Dto.Id.HasValue && model.Dto.Id.Value > 0)
                {
                    var entity = await scopedRepo.FindAsync(model.Dto.Id.Value);
                    if (entity == null)
                    {
                        rs.AddError("Kayıt bulunamadı");
                        return rs;
                    }

                    _mapper.Map(model.Dto, entity);
                    entity.ModifiedDate = DateTime.Now;
                    entity.ModifiedId = model.UserId;
                    scopedRepo.Update(entity);
                }
                else
                {
                    var entity = _mapper.Map<Branch>(model.Dto);
                    entity.CreatedDate = DateTime.Now;
                    entity.CreatedId = model.UserId;
                    entity.Status = (int)EntityStatus.Active;
                    await scopedRepo.InsertAsync(entity);
                }

                await scopedUow.SaveChangesAsync();
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBranch Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteBranch(int id, int userId)
        {
            var rs = new IActionResult<Empty>();
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<Branch>();

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
                _logger.LogError("DeleteBranch Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }
    }
}
