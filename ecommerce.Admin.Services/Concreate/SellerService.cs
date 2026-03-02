using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SellerDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ecommerce.Core.Entities.Hierarchical;

namespace ecommerce.Admin.Domain.Concreate
{
    public class SellerService : ISellerService
    {
        private const string MENU_NAME = "sellers";
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Seller> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        

        public SellerService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory scopeFactory, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<Seller>();
            _mapper = mapper;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }

        private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
        private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
        private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
        private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

        public async Task<IActionResult<Paging<List<SellerListDto>>>> GetSellers(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<SellerListDto>>>();
            try
            {
                var query = _repository.GetAll(true)
                    .IgnoreQueryFilters()
                    .Where(s => s.Status == (int)EntityStatus.Active);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                query = query
                    .Include(s => s.City)
                    .Include(s => s.Town);
                response.Result = await query.ToPagedResultAsync<SellerListDto>(pager, _mapper);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSellers Exception {Message}", ex);
                response.AddSystemError(ex.Message);
            }
            return response;
        }

        public async Task<IActionResult<SellerUpsertDto>> GetSellerById(int id)
        {
            var rs = new IActionResult<SellerUpsertDto> { Result = new SellerUpsertDto() };
            try
            {
                if (!await CanView())
                {
                    rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return rs;
                }

                var query = _repository.GetAll(true)
                    .IgnoreQueryFilters()
                    .Where(f => f.Id == id);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                var seller = await query
                    .Include(s => s.City)
                    .Include(s => s.Town)
                    .FirstOrDefaultAsync();

                if (seller == null)
                {
                    rs.AddError("Satıcı bulunamadı veya yetkiniz yok");
                    return rs;
                }

                if (!await _roleFilter.CanAccessBranchAsync(seller.BranchId ?? 0, _context.DbContext))
                {
                     rs.AddError("Bu şubeye erişim yetkiniz yok.");
                     return rs;
                }

                rs.Result = _mapper.Map<SellerUpsertDto>(seller);
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSellerById Exception {Message}", ex);
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<int>> UpsertSeller(AuditWrapDto<SellerUpsertDto> model)
        {
            var response = new IActionResult<int> { Result = 0 };
            try
            {
                var dto = model.Dto;
                var repo = _repository;

                if (!dto.Id.HasValue)
                {
                    if (!await CanCreate())
                    {
                        response.AddError("Ekleme yetkiniz bulunmamaktadır.");
                        return response;
                    }

                    // Duplicate Check
                    var existingQuery = repo.GetAll(true).IgnoreQueryFilters().Where(x => x.Name == dto.Name && x.Status != (int)EntityStatus.Passive);
                    existingQuery = _roleFilter.ApplyFilter(existingQuery, _context.DbContext);
                    var exists = await existingQuery.AnyAsync();
                    
                    if (exists)
                    {
                        response.AddError("Bu isimde bir satıcı zaten mevcut.");
                        return response;
                    }

                    var entity = _mapper.Map<Seller>(dto);
                    if (_tenantProvider.IsMultiTenantEnabled)
                    {
                        entity.BranchId = _tenantProvider.GetCurrentBranchId();
                    }
                    await repo.InsertAsync(entity);
                    response.Result = entity.Id;
                }
                else
                {
                    if (!await CanEdit())
                    {
                        response.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                        return response;
                    }

                    var query = repo.GetAll(true)
                        .IgnoreQueryFilters()
                        .Where(x => x.Id == dto.Id);
                    
                    query = _roleFilter.ApplyFilter(query, _context.DbContext);
                    
                    var current = await query.FirstOrDefaultAsync();

                    if (current == null)
                    {
                        response.AddError("Satıcı bulunamadı veya yetkiniz yok");
                        return response;
                    }

                    if (!await _roleFilter.CanAccessBranchAsync(current.BranchId ?? 0, _context.DbContext))
                    {
                        response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return response;
                    }

                    current.Name = dto.Name;
                    current.Email = dto.Email;
                    current.PhoneNumber = dto.PhoneNumber;
                    current.Address = dto.Address;
                    current.Commission = dto.Commission;
                    current.CityId = dto.CityId;
                    current.TownId = dto.TownId;
                    current.TaxOffice = dto.TaxOffice;
                    current.TaxNumber = dto.TaxNumber;
                    current.IsOrderUse = dto.IsOrderUse;
                    current.Status = dto.Status;
                    // Preserve BranchId
                    
                    repo.Update(current);
                    response.Result = current.Id;
                }

                await _context.SaveChangesAsync();
                var last = _context.LastSaveChangesResult;
                if (last.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }
                if (last.Exception != null)
                {
                    response.AddError(last.Exception.ToString());
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertSeller Exception {Message}", ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> DeleteSeller(AuditWrapDto<SellerDeleteDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await CanDelete())
                {
                    rs.AddError("Silme yetkiniz bulunmamaktadır.");
                    return rs;
                }

                var query = _repository.GetAll(true)
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == model.Dto.Id);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var seller = await query.FirstOrDefaultAsync();
                
                if (seller == null)
                {
                    rs.AddError("Satıcı bulunamadı veya yetkiniz yok");
                    return rs;
                }

                if (!await _roleFilter.CanAccessBranchAsync(seller.BranchId ?? 0, _context.DbContext))
                {
                     rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return rs;
                }

                _context.DbContext.Sellers.Remove(seller);
                await _context.SaveChangesAsync();
                var last = _context.LastSaveChangesResult;
                if (last.IsOk)
                {
                    rs.AddSuccess("Successfull");
                    return rs;
                }
                if (last.Exception != null)
                {
                    rs.AddError(last.Exception.ToString());
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteSeller Exception {Message}", ex);
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<List<int>> GetAllSellerIds()
        {
            try
            {
                // The repository uses DbContext which has Global Query Filter for BranchId.
                // So this will return only sellers for the current branch.
                return await _repository.GetAll(true).Select(s => s.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllSellerIds Exception");
                return new List<int>();
            }
        }

    }
}

