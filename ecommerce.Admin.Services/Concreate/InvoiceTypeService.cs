using AutoMapper;
using ecommerce.Admin.Domain.Dtos.InvoiceTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Domain.Concreate
{
    public class InvoiceTypeService : IInvoiceTypeService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<InvoiceTypeDefinition> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<InvoiceTypeListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "invoicetypes";

        public InvoiceTypeService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<InvoiceTypeListDto> radzenPagerService, IServiceScopeFactory serviceScopeFactory, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<InvoiceTypeDefinition>();
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

        public async Task<IActionResult<Paging<IQueryable<InvoiceTypeListDto>>>> GetInvoiceTypes(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<InvoiceTypeListDto>>> response = new() { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(ignoreQueryFilters: true)
                    .Where(x => x.Status == (int)EntityStatus.Active);

                // Tenant (BranchId) filtreleme — listede sadece seçili şubenin kayıtları (çift görünmeyi önlemek için global 0 karıştırılmaz)
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                if (currentBranchId > 0)
                {
                    query = query.Where(x => x.BranchId == currentBranchId);
                }
                else
                {
                    // Merkez Ofis (0) seçiliyse sadece BranchId=0 kayıtları
                    query = query.Where(x => x.BranchId == 0);
                }

                var items = await query.ToListAsync();
                var mapped = _mapper.Map<List<InvoiceTypeListDto>>(items);
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
                _logger.LogError("GetInvoiceTypes Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<InvoiceTypeListDto>>> GetInvoiceTypes()
        {
            var response = new IActionResult<List<InvoiceTypeListDto>> { Result = new List<InvoiceTypeListDto>() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = ctx.GetRepository<InvoiceTypeDefinition>();

                var query = repo.GetAll(disableTracking: true, ignoreQueryFilters: true)
                    .Where(x => x.Status == (int)EntityStatus.Active);

                // Tenant (BranchId) filtreleme — sadece seçili şube
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                if (currentBranchId > 0)
                {
                    query = query.Where(x => x.BranchId == currentBranchId);
                }
                else
                {
                    query = query.Where(x => x.BranchId == 0);
                }

                var items = await query.ToListAsync();
                var mapped = _mapper.Map<List<InvoiceTypeListDto>>(items);
                if (mapped?.Count > 0) response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetInvoiceTypes Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        /// <summary>
        /// Fatura tipi listesini yetki kontrolü olmadan, sadece BranchId filtreleme ile döndürür.
        /// Fatura oluşturma modal'ından çağrılır.
        /// Global query filter bypass edilir, doğrudan BranchId ile filtrelenir.
        /// </summary>
        public async Task<IActionResult<List<InvoiceTypeListDto>>> GetInvoiceTypesForInvoice()
        {
            var response = new IActionResult<List<InvoiceTypeListDto>> { Result = new List<InvoiceTypeListDto>() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = ctx.GetRepository<InvoiceTypeDefinition>();

                var query = repo.GetAll(disableTracking: true, ignoreQueryFilters: true)
                    .Where(x => x.Status == (int)EntityStatus.Active);

                // Fatura modalında çift görünmeyi önlemek için sadece seçili şubenin kayıtları (liste ile aynı mantık)
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                if (currentBranchId > 0)
                {
                    query = query.Where(x => x.BranchId == currentBranchId);
                }
                else
                {
                    query = query.Where(x => x.BranchId == 0);
                }
                
                var items = await query.ToListAsync();
                var mapped = _mapper.Map<List<InvoiceTypeListDto>>(items);
                if (mapped?.Count > 0) response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetInvoiceTypesForInvoice Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<InvoiceTypeUpsertDto>> GetInvoiceTypeById(int id)
        {
            var response = new IActionResult<InvoiceTypeUpsertDto> { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(predicate: x => x.Id == id, ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                    response.AddError("Kayıt bulunamadı veya yetkiniz yok.");
                    return response;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                     response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return response;
                }
                var mapped = _mapper.Map<InvoiceTypeUpsertDto>(entity);
                if (mapped != null) response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetInvoiceTypeById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertInvoiceType(AuditWrapDto<InvoiceTypeUpsertDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    if (!await CanCreate())
                    {
                        response.AddError("Ekleme yetkiniz bulunmamaktadır.");
                        return response;
                    }

                    var entity = _mapper.Map<InvoiceTypeDefinition>(dto);
                    entity.BranchId = _tenantProvider.GetCurrentBranchId();
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    if (!await CanEdit())
                    {
                        response.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                        return response;
                    }

                    var query = _repository.GetAll(predicate: x => x.Id == dto.Id, ignoreQueryFilters: true);
                    
                    query = _roleFilter.ApplyFilter(query, _context.DbContext);
                    
                    var current = await query.FirstOrDefaultAsync();

                    if (current == null)
                    {
                        response.AddError("Fatura tipi bulunamadı veya yetkiniz yok");
                        return response;
                    }

                    if (!await _roleFilter.CanAccessBranchAsync(current.BranchId, _context.DbContext))
                    {
                         response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return response;
                    }

                    var updated = _mapper.Map<InvoiceTypeDefinition>(dto);
                    updated.Id = current.Id;
                    updated.BranchId = current.BranchId; // Preserve branch
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
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertInvoiceType Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> DeleteInvoiceType(AuditWrapDto<InvoiceTypeDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await CanDelete())
                {
                    response.AddError("Silme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(predicate: x => x.Id == model.Dto.Id, ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                    response.AddError("Kayıt bulunamadı veya yetkiniz yok.");
                    return response;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                     response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return response;
                }

                await _context.DbContext.InvoiceTypes.Where(f => f.Id == model.Dto.Id)
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
                _logger.LogError($"DeleteInvoiceType Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}

