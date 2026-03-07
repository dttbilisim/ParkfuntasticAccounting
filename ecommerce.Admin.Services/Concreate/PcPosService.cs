using AutoMapper;
using ecommerce.Admin.Domain.Dtos.PcPosDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ecommerce.Core.Interfaces;

namespace ecommerce.Admin.Domain.Concreate
{
    public class PcPosService : IPcPosService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<PcPosDefinition> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<PcPosListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "PcPos";

        public PcPosService(
            IUnitOfWork<ApplicationDbContext> context, 
            IMapper mapper, 
            ILogger logger, 
            IRadzenPagerService<PcPosListDto> radzenPagerService, 
            IServiceScopeFactory serviceScopeFactory, 
            ITenantProvider tenantProvider, 
            IHttpContextAccessor httpContextAccessor,
            ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<PcPosDefinition>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }

        private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
        private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
        private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
        private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

        public async Task<IActionResult<Paging<IQueryable<PcPosListDto>>>> GetPcPos(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<PcPosListDto>>> response = new() { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: x => x.Status == (int)EntityStatus.Active,
                    include: i => i.Include(x => x.PaymentType),
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var items = await query.ToListAsync();
                var mapped = _mapper.Map<List<PcPosListDto>>(items);
                if (mapped?.Count > 0)
                {
                    response.Result.Data = mapped.AsQueryable();
                    response.Result.Data = response.Result.Data?.OrderByDescending(x => x.Id);
                }

                response.Result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPcPos Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<PcPosListDto>>> GetPcPos()
        {
            var response = new IActionResult<List<PcPosListDto>> { Result = new List<PcPosListDto>() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = ctx.GetRepository<PcPosDefinition>();
                var roleFilter = scope.ServiceProvider.GetRequiredService<ecommerce.Admin.Domain.Services.IRoleBasedFilterService>();

                var query = repo.GetAll(
                    predicate: x => x.Status == (int)EntityStatus.Active,
                    disableTracking: true,
                    include: i => i.Include(x => x.PaymentType),
                    ignoreQueryFilters: true);
                
                query = roleFilter.ApplyFilter(query, ctx.DbContext);
                
                var items = await query.ToListAsync();
                var mapped = _mapper.Map<List<PcPosListDto>>(items);
                if (mapped?.Count > 0) response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPcPos Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<PcPosListDto>>> GetPcPosForUserAssignment()
        {
            var response = new IActionResult<List<PcPosListDto>> { Result = new List<PcPosListDto>() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = ctx.GetRepository<PcPosDefinition>();

                // Kullanıcı ataması için role filter uygulanmıyor - tüm aktif PcPos tanımları gösterilir
                var query = repo.GetAll(
                    predicate: x => x.Status == (int)EntityStatus.Active,
                    disableTracking: true,
                    include: i => i.Include(x => x.PaymentType),
                    ignoreQueryFilters: true);

                var items = await query.OrderBy(x => x.Name).ToListAsync();
                var mapped = _mapper.Map<List<PcPosListDto>>(items);
                if (mapped?.Count > 0) response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPcPosForUserAssignment Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<PcPosUpsertDto>> GetPcPosById(int id)
        {
            var response = new IActionResult<PcPosUpsertDto> { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: f => f.Id == id, 
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                    // Check existance for security msg
                    var exists = await _repository.GetAll(predicate: x => x.Id == id, ignoreQueryFilters: true).AnyAsync();
                    if (exists)
                    {
                         response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return response;
                    }
                    return response;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                        response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return response;
                }
                var mapped = _mapper.Map<PcPosUpsertDto>(entity);
                if (mapped != null) response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPcPosById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertPcPos(AuditWrapDto<PcPosUpsertDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue || dto.Id == 0)
                {
                    if (!await CanCreate())
                    {
                        response.AddError("Ekleme yetkiniz bulunmamaktadır.");
                        return response;
                    }

                    var entity = _mapper.Map<PcPosDefinition>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;

                    // Ensure tenant IDs are set if not provided (though UI should provide them)
                    // If they are 0, we can default to 0 or leave them as is (assuming UI requires selection)
                    // Given the request "sirket ve subelere bagla", we expect valid IDs.
                    
                    // Auto-assign BranchId if not set
                    if (entity.BranchId == 0)
                        entity.BranchId = _tenantProvider.GetCurrentBranchId();
                    
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    if (!await CanEdit())
                    {
                        response.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                        return response;
                    }

                    var query = _repository.GetAll(
                        predicate: x => x.Id == dto.Id,
                        disableTracking: false, // Need tracking for update
                        ignoreQueryFilters: true);
                    
                    query = _roleFilter.ApplyFilter(query, _context.DbContext);
                    
                    var entity = await query.FirstOrDefaultAsync();

                    if (entity == null)
                    {
                        response.AddError("PcPos kaydı bulunamadı veya yetkiniz yok.");
                        return response;
                    }

                    if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                    {
                         response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return response;
                    }
                    
                    // Standard update pattern: Map DTO to Entity
                    _mapper.Map(dto, entity);
                    
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.ModifiedId = model.UserId;
                    entity.ModifiedDate = DateTime.Now;
                    
                    _repository.Update(entity);
                }

                await _context.SaveChangesAsync();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertPcPos Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> DeletePcPos(AuditWrapDto<PcPosDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await CanDelete())
                {
                    response.AddError("Silme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: f => f.Id == model.Dto.Id, 
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();
                
                if (entity != null)
                {
                     if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                     {
                          response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                          return response;
                     }

                     entity.DeletedId = model.UserId;
                     entity.DeletedDate = DateTime.Now;
                     entity.Status = (int)EntityStatus.Deleted;
                     _repository.Update(entity);
                     await _context.SaveChangesAsync();
                }
                else
                {
                    response.AddError("Kayıt bulunamadı veya yetkiniz yok.");
                    return response;
                }

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
                _logger.LogError($"DeletePcPos Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}

