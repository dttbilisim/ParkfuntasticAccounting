using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Npgsql;

namespace ecommerce.Admin.Services.Concreate
{
    public class CashRegisterService : ICashRegisterService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<CashRegisterService> _logger;
        private readonly IRadzenPagerService<CashRegisterListDto> _radzenPagerService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "cash-registers";

        public CashRegisterService(
            IServiceScopeFactory serviceScopeFactory, 
            IMapper mapper, 
            ILogger<CashRegisterService> logger,
            IRadzenPagerService<CashRegisterListDto> radzenPagerService,
            ITenantProvider tenantProvider,
            IHttpContextAccessor httpContextAccessor,
            ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Paging<IQueryable<CashRegisterListDto>>>> GetCashRegisters(PageSetting pager)
        {
            var rs = new IActionResult<Paging<IQueryable<CashRegisterListDto>>> { Result = new() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<CashRegister>();
                
                var query = scopedRepo.GetAll(
                    predicate: x => x.Status != (int)EntityStatus.Deleted,
                    include: i => i.Include(x => x.Currency).Include(x => x.PaymentType),
                    ignoreQueryFilters: true);

                query = _roleFilter.ApplyFilter(query, scopedUow.DbContext);

                var mappedList = _mapper.Map<List<CashRegisterListDto>>(await query.ToListAsync());
                rs.Result.Data = mappedList.AsQueryable();

                var pagingResult = _radzenPagerService.MakeDataQueryable(rs.Result.Data, pager);
                rs.Result = pagingResult;
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCashRegisters Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<List<CashRegisterListDto>>> GetCashRegisters()
        {
            var rs = new IActionResult<List<CashRegisterListDto>> { Result = new() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<CashRegister>();

                var query = scopedRepo.GetAll(
                    predicate: x => x.Status == (int)EntityStatus.Active,
                    include: i => i.Include(x => x.Currency).Include(x => x.PaymentType),
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, scopedUow.DbContext);

                var entities = await query.ToListAsync();

                rs.Result = _mapper.Map<List<CashRegisterListDto>>(entities);
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCashRegisters List Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<CashRegisterUpsertDto>> GetCashRegisterById(int id)
        {
            var rs = new IActionResult<CashRegisterUpsertDto>();
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<CashRegister>();
                var branchRepo = scopedUow.GetRepository<Branch>();

                var query = scopedRepo.GetAll(true).IgnoreQueryFilters().Where(x => x.Id == id);
                query = _roleFilter.ApplyFilter(query, scopedUow.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                     rs.AddError("Kasa/banka bulunamadı veya yetkiniz yok.");
                     return rs;
                }

                rs.Result = _mapper.Map<CashRegisterUpsertDto>(entity);
                var branch = await branchRepo.GetAll(true).Where(b => b.Id == entity.BranchId).Select(b => new { b.CorporationId }).FirstOrDefaultAsync();
                if (branch != null)
                    rs.Result.CorporationId = branch.CorporationId;
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCashRegisterById Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertCashRegister(AuditWrapDto<CashRegisterUpsertDto> model)
        {
            var rs = new IActionResult<Empty>();
            try
            {
                var dto = model.Dto;
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<CashRegister>();

                if (model.Dto.IsDefault)
                {
                    var defaults = await scopedRepo.GetAllAsync(predicate: x => x.IsDefault && x.Status != (int)EntityStatus.Deleted);
                    foreach (var item in defaults)
                    {
                        item.IsDefault = false;
                        scopedRepo.Update(item);
                    }
                }

                if (!dto.Id.HasValue || dto.Id == 0)
                {
                    // CREATE
                    if (!await _permissionService.CanCreate(MENU_NAME))
                    {
                        rs.AddError("Ekleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }

                    // Check for duplicate name in current branch
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    var duplicate = await scopedRepo.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(cr => cr.Name.ToLower() == dto.Name.ToLower() && cr.BranchId == currentBranchId && cr.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli kasa/banka bu şubede zaten mevcut.");
                        return rs;
                    }

                    var entity = _mapper.Map<CashRegister>(model.Dto);
                    
                    // Set tenant IDs if not provided
                    if (entity.BranchId == 0) entity.BranchId = currentBranchId;

                    entity.CreatedDate = DateTime.Now;
                    entity.CreatedId = model.UserId;
                    entity.Status = (int)EntityStatus.Active;
                    await scopedRepo.InsertAsync(entity);
                }
                else
                {
                    // UPDATE
                    if (!await _permissionService.CanEdit(MENU_NAME))
                    {
                        rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }

                    var query = scopedRepo.GetAll(true).IgnoreQueryFilters().Where(x => x.Id == model.Dto.Id.Value);
                    query = _roleFilter.ApplyFilter(query, scopedUow.DbContext);
                    
                    var entity = await query.FirstOrDefaultAsync();

                    // Logic moved to global service
                    if (entity == null)
                    {
                        rs.AddError("Kayıt bulunamadı veya yetkiniz yok");
                        return rs;
                    }

                    if(!await _roleFilter.CanAccessBranchAsync(entity.BranchId, scopedUow.DbContext))
                    {
                        rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return rs;
                    }

                    // Check for duplicate name in same branch (excluding current entity)
                    var duplicate = await scopedRepo.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(cr => cr.Id != dto.Id && cr.Name.ToLower() == dto.Name.ToLower() && cr.BranchId == entity.BranchId && cr.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli kasa/banka bu şubede zaten mevcut.");
                        return rs;
                    }

                    _mapper.Map(model.Dto, entity);
                    entity.ModifiedDate = DateTime.Now;
                    entity.ModifiedId = model.UserId;
                    scopedRepo.Update(entity);
                }

                await scopedUow.SaveChangesAsync();
                var lastResult = scopedUow.LastSaveChangesResult;
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
                            rs.AddError($"'{dto.Name}' isimli kasa/banka zaten mevcut.");
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
                _logger.LogError("UpsertCashRegister Error: {Ex}", ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    rs.AddError($"'{model.Dto.Name}' isimli kasa/banka zaten mevcut.");
                }
                else
                {
                    rs.AddError(ex.Message);
                }
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteCashRegister(AuditWrapDto<CashRegisterDeleteDto> model)
        {
            var rs = new IActionResult<Empty>();
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var scopedRepo = scopedUow.GetRepository<CashRegister>();

                if (!await _permissionService.CanDelete(MENU_NAME))
                {
                    rs.AddError("Silme yetkiniz bulunmamaktadır.");
                    return rs;
                }

                var query = scopedRepo.GetAll(true).IgnoreQueryFilters().Where(x => x.Id == model.Dto.Id);
                query = _roleFilter.ApplyFilter(query, scopedUow.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();
                
                if (entity == null)
                {
                    rs.AddError("Kayıt bulunamadı veya yetkiniz yok");
                    return rs;
                }

                if(!await _roleFilter.CanAccessBranchAsync(entity.BranchId, scopedUow.DbContext))
                {
                    rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                    return rs;
                }

                if (entity != null)
                {
                    entity.Status = (int)EntityStatus.Deleted;
                    entity.DeletedDate = DateTime.Now;
                    entity.DeletedId = model.UserId;
                    scopedRepo.Update(entity);
                    await scopedUow.SaveChangesAsync();
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteCashRegister Error: {Ex}", ex.ToString());
                rs.AddError(ex.Message);
                return rs;
            }
        }

    }
}
