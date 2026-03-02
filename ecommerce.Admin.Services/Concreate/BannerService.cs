using AutoMapper;
using ecommerce.Admin.Domain.Dtos.BannerDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.Core.Entities;
using Microsoft.Extensions.Logging;
using ecommerce.Admin.Domain.Extensions;
using Microsoft.EntityFrameworkCore;
using ecommerce.Core.Extensions;
using ecommerce.EFCore.UnitOfWork;
using ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
namespace ecommerce.Admin.Domain.Concreate
{
    public class BannerService : IBannerService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Banner> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<BannerListDto> _radzenPagerService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "Banners";

        public BannerService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<BannerListDto> radzenPagerService, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor, IServiceScopeFactory serviceScopeFactory, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<Banner>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _serviceScopeFactory = serviceScopeFactory;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }

        private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
        private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
        private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
        private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);
        public async Task<IActionResult<Paging<List<BannerListDto>>>> GetBanners(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<BannerListDto>>>();

            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(true)
                    .IgnoreQueryFilters()
                    .Where(s => s.Status != (int)EntityStatus.Deleted);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                response.Result = await query.ToPagedResultAsync<BannerListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetBanners Exception " + e);
                response.AddSystemError(e.Message);
            }

            return response;
        }

        public async Task<IActionResult<Empty>> DeleteBanner(AuditWrapDto<BannerDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                if (!await CanDelete())
                {
                    rs.AddError("Silme yetkiniz bulunmamaktadır.");
                    return rs;
                }

                var control = await _context.DbContext.BannerItems.AsNoTracking().Where(x => x.BannerId == model.Dto.Id && x.Status == (int)EntityStatus.Active).AnyAsync();
                if (control)
                {
                    rs.AddError("Bu banneri silemezsiniz. Bu banner ait öğerler mevcut");
                    return rs;
                }

                var query = _context.DbContext.Banners.IgnoreQueryFilters().Where(f => f.Id == model.Dto.Id);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var banner = await query.FirstOrDefaultAsync();

                if (banner == null)
                {
                    rs.AddError("Banner bulunamadı veya yetkiniz yok.");
                    return rs;
                }

                if (!await _roleFilter.CanAccessBranchAsync(banner.BranchId ?? 0, _context.DbContext))
                {
                    rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                    return rs;
                }

                banner.Status = (int)EntityStatus.Deleted;
                banner.DeletedDate = DateTime.Now;
                banner.DeletedId = model.UserId;

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
                _logger.LogError("DeleteBanner Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<int> GetBannerLastCount(BannerType bannertypeId)
        {
            var Banner = await _context.DbContext.Banners.OrderByDescending(x => x.Order).FirstOrDefaultAsync(x => x.BannerType == bannertypeId && x.Status!=99);
            return (Banner?.Order??0)+1;
        }
        public async Task<IActionResult<BannerUpsertDto>> GetBannerById(int Id)
        {
            var rs = new IActionResult<BannerUpsertDto>
            {
                Result = new BannerUpsertDto()
            };
            try
            {
                if (!await CanView())
                {
                    rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return rs;
                }
                var query = _repository.GetAll(
                    predicate: f => f.Id == Id,
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var Banner = await query.FirstOrDefaultAsync();

                if (Banner == null)
                {
                    var exists = await _repository.GetAll(predicate: f => f.Id == Id, ignoreQueryFilters: true).AnyAsync();
                    if (exists)
                    {
                        rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return rs;
                    }

                    rs.AddError("Banner Bulunamadı");
                    return rs;
                }

                if (!await _roleFilter.CanAccessBranchAsync(Banner.BranchId ?? 0, _context.DbContext))
                {
                        rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return rs;
                }

                var mappedCat = _mapper.Map<BannerUpsertDto>(Banner);
                if (mappedCat != null)
                {
                    rs.Result = mappedCat;
                }
                else rs.AddError("Banner Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBanner Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertBanner(AuditWrapDto<BannerUpsertDto> model)
        {
            //The instance of entity type 'Banner' cannot be tracked because another instance with the same key value for { 'Id'} is already being tracked.When attaching existing entities, ensure that only one entity instance with a given key value is attached.Consider using


            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;

                if (!dto.Id.HasValue)
                {
                    if (!await CanCreate())
                    {
                        rs.AddError("Ekleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }
                }
                else
                {
                    if (!await CanEdit())
                    {
                        rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }
                }

                var entity = _mapper.Map<Banner>(dto);
                if (!dto.Id.HasValue)
                {
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    if (_tenantProvider.IsMultiTenantEnabled)
                    {
                        entity.BranchId = _tenantProvider.GetCurrentBranchId();
                    }
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    var query = _context.DbContext.Banners.IgnoreQueryFilters().Where(x => x.Id == dto.Id);
                    
                    query = _roleFilter.ApplyFilter(query, _context.DbContext);
                    
                    entity = await query.FirstOrDefaultAsync();
                    
                    if (entity == null)
                    {
                        rs.AddError("Banner bulunamadı veya yetkiniz yok.");
                        return rs;
                    }

                    if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId ?? 0, _context.DbContext))
                    {
                         rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return rs;
                    }

                    entity.Name = dto.Name;
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.Order = dto.Order;
                    entity.AutoStartTime = dto.AutoStartTime;
                    entity.AutoLoop = dto.AutoLoop;
                    entity.ReplayTime = dto.ReplayTime;
                    entity.BannerType = dto.BannerType;
                    entity.ModifiedId = model.UserId;
                    entity.ModifiedDate = DateTime.Now;
                    // Preserve BranchId
                  
                    _repository.Update(entity);
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
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBanner Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<BannerListDto>>> GetBanners()
        {
            IActionResult<List<BannerListDto>> response = new() { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: f => f.Status == (int)EntityStatus.Active,
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var Banners = await query.ToListAsync();

                var mappedCats = _mapper.Map<List<BannerListDto>>(Banners);
                if (mappedCats != null)
                {
                    if (mappedCats.Count > 0)
                        response.Result = mappedCats.ToList();
                }
                foreach (var mdl in response.Result)
                {
                    mdl.Name = mdl.Name + " > " + ((BannerType)mdl.BannerType).GetDisplayName();
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductForParentSelectList Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}
