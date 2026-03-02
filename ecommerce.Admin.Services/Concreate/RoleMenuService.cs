using AutoMapper;
using ecommerce.Admin.Domain.Dtos.RoleMenuDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class RoleMenuService : IRoleMenuService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<RoleMenu> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<RoleMenuListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RoleMenuService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<RoleMenuListDto> radzenPagerService, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _repository = context.GetRepository<RoleMenu>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<IActionResult<Empty>> DeleteRoleMenu(AuditWrapDto<RoleMenuDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {


                //Deleted Mark with audit
                await _context.DbContext.RoleMenus.Where(f => f.Id == model.Dto.Id)
                    .ExecuteDeleteAsync();

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Silme ??lemi Ba?ar?l?");
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
                _logger.LogError("DeleteRoleMenu Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<RoleMenuUpsertDto>> GetRoleMenuById(int Id)
        {
            var rs = new IActionResult<RoleMenuUpsertDto>
            {
                Result = new()
            };
            try
            {
                var data = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<RoleMenuUpsertDto>(data);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetRoleMenuById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<RoleMenuListDto>>>> GetRoleMenus(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<RoleMenuListDto>>> response = new() { Result = new() };

            try
            {
                var datas = await _repository.GetAllAsync(predicate: null);
                var mapped = _mapper.Map<List<RoleMenuListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        response.Result.Data = mapped.AsQueryable();
                }

                if (response.Result.Data != null)
                    response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);

                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);

                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetRoleMenus Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<RoleMenuListDto>>> GetRoleMenus(List<int> RoleIds)
        {
            var rs = new IActionResult<List<RoleMenuListDto>>
            {
                Result = new List<RoleMenuListDto>()
            };
            try
            {
                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    var datas = await dbContext.Set<RoleMenu>()
                        .AsNoTracking()
                        .Where(x => RoleIds.Contains(x.RoleId))
                        .ToListAsync();
                    var mapped = _mapper.Map<List<RoleMenuListDto>>(datas);
                    if (mapped != null)
                    {
                        if (mapped.Count > 0)
                            rs.Result = mapped;
                    }
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetRoleMenus Exception " + ex.ToString());

                rs.AddError("Liste Alınamadı");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertRoleMenu(AuditWrapDto<RoleMenuUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                var entity = _mapper.Map<RoleMenu>(dto);
                if (!dto.Id.HasValue)
                {
                    await _repository.InsertAsync(entity);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.DbContext.RoleMenus.Where(f => f.Id == model.Dto.Id).
                        ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.RoleId, dto.RoleId)
                        .SetProperty(a => a.MenuId, dto.MenuId));

                }
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Kayıt Işlemi Başarılı");
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
                _logger.LogError("UpsertRoleMenu Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<RoleMenuListDto>>> GetRoleMenus(int roleId)
        {
             var rs = new IActionResult<List<RoleMenuListDto>>
             {
                 Result = new List<RoleMenuListDto>()
             };
             try
             {
                 using (var scope = _serviceScopeFactory.CreateScope())
                 {
                     var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                     
                     var datas = await dbContext.Set<RoleMenu>()
                         .AsNoTracking()
                         .Where(x => x.RoleId == roleId)
                         .ToListAsync();
                     var mapped = _mapper.Map<List<RoleMenuListDto>>(datas);
                     if (mapped != null && mapped.Count > 0)
                     {
                         rs.Result = mapped;
                     }
                 }
                 return rs;
             }
             catch (Exception ex)
             {
                 _logger.LogError("GetRoleMenus(int) Exception " + ex.ToString());
                 rs.AddSystemError(ex.ToString());
                 return rs;
             }
        }

        public async Task<IActionResult<Empty>> UpsertRoleMenus(int roleId, List<RoleMenuUpsertDto> roleMenus)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                     var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                     var repo = uow.GetRepository<RoleMenu>();
                     
                     // Delete existing role menus for this role
                     var existingMenus = await repo.GetAllAsync(predicate: x => x.RoleId == roleId);
                     foreach (var menu in existingMenus)
                     {
                         repo.Delete(menu);
                     }
     
                     // Insert new role menus with permissions
                     foreach (var dto in roleMenus)
                     {
                         await repo.InsertAsync(new RoleMenu
                         {
                             RoleId = roleId,
                             MenuId = dto.MenuId,
                             CanView = dto.CanView,
                             CanCreate = dto.CanCreate,
                             CanEdit = dto.CanEdit,
                             CanDelete = dto.CanDelete
                         });
                     }
     
                     await uow.SaveChangesAsync();
                     var lastResult = uow.LastSaveChangesResult;
                     
                     if (lastResult.IsOk)
                     {
                         rs.AddSuccess("Rol menü yetkileri başarıyla güncellendi");
                         return rs;
                     }
                     else
                     {
                         if (lastResult.Exception != null)
                             rs.AddError(lastResult.Exception.ToString());
                         return rs;
                     }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertRoleMenus Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
