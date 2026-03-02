using AutoMapper;
using ecommerce.Admin.Domain.Dtos.UserDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ecommerce.Core.Interfaces;

namespace ecommerce.Admin.Domain.Concreate
{
    public class UserService : IUserService
    {
        private const string MENU_NAME = "users";
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<User> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory scopeFactory, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _repository = context.GetRepository<User>();
            _mapper = mapper;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult<Paging<List<UserListDto>>>> GetUsers(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<UserListDto>>>();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<User>();
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;

                var query = repo.GetAll(true);

                if (!isGlobalAdmin && user != null)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        var allowedBranchIds = uow.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();

                        query = query.Where(u => uow.DbContext.UserBranches.Any(ub => ub.UserId == u.Id && allowedBranchIds.Contains(ub.BranchId)));
                    }
                }

                response.Result = await query.ToPagedResultAsync<UserListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetUsers Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }

        public async Task<IActionResult<UserUpsertDto>> GetUserById(int id)
        {
            var rs = new IActionResult<UserUpsertDto> { Result = new() };
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<User>();
                var user = await repo.GetFirstOrDefaultAsync(predicate: f => f.Id == id);
                var mapped = _mapper.Map<UserUpsertDto>(user);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                else rs.AddError("Kullanıcı bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUserById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<int>> UpsertUser(AuditWrapDto<UserUpsertDto> model)
        {
            var response = new IActionResult<int> { Result = 0 };
            try
            {
                var dto = model.Dto;
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<User>();
                if (!dto.Id.HasValue)
                {
                    var entity = _mapper.Map<User>(dto);
                    await repo.InsertAsync(entity);
                    response.Result = entity.Id;
                }
                else
                {
                    var current = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id,
                        disableTracking: true);
                    if (current == null)
                    {
                        response.AddError("Kullanıcı bulunamadı");
                        return response;
                    }
                    var updated = _mapper.Map<User>(dto);
                    updated.Id = current.Id;
                    repo.AttachAsModified(updated, excludeNavigations: true);
                    response.Result = updated.Id;
                }
                await uow.SaveChangesAsync();
                var last = uow.LastSaveChangesResult;
                if (last.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }
                if (last.Exception != null) response.AddError(last.Exception.ToString());
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertUser Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> DeleteUser(AuditWrapDto<UserDeleteDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                await uow.DbContext.Users.Where(f => f.Id == model.Dto.Id)
                    .ExecuteDeleteAsync();
                await uow.SaveChangesAsync();
                var last = uow.LastSaveChangesResult;
                if (last.IsOk)
                {
                    rs.AddSuccess("Successfull");
                    return rs;
                }
                if (last.Exception != null) rs.AddError(last.Exception.ToString());
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteUser Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<int>> GetUserCount()
        {
            var rs = new IActionResult<int> { Result = 0 };
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;

                var query = uow.DbContext.Users.AsNoTracking();

                var user = _httpContextAccessor.HttpContext?.User;

                if (!isGlobalAdmin && user != null)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        var allowedBranchIds = await uow.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToListAsync();

                        // Only count users who are assigned to at least one of the current user's branches
                        query = query.Where(u => uow.DbContext.UserBranches.Any(ub => ub.UserId == u.Id && allowedBranchIds.Contains(ub.BranchId)));
                    }
                }

                rs.Result = await query.CountAsync();
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetUserCount Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}


