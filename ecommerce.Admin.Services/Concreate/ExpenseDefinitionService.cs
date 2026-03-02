using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ExpenseDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Npgsql;
namespace ecommerce.Admin.Services.Concreate;

public class ExpenseDefinitionService : IExpenseDefinitionService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<ExpenseDefinition> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IRadzenPagerService<ExpenseDefinitionListDto> _radzenPagerService;
    private readonly ITenantProvider _tenantProvider;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "Expenses";

    public ExpenseDefinitionService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
        IRadzenPagerService<ExpenseDefinitionListDto> radzenPagerService,
        ITenantProvider tenantProvider,
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory,
        ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _context = context;
        _repository = context.GetRepository<ExpenseDefinition>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
        _roleFilter = roleFilter;
        _permissionService = permissionService;
    }

    private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
    private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
    private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
    private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

    public async Task<IActionResult<Paging<IQueryable<ExpenseDefinitionListDto>>>> GetMainExpenses(PageSetting pager, ExpenseOperationType operationType)
    {
        IActionResult<Paging<IQueryable<ExpenseDefinitionListDto>>> response = new() { Result = new() };
        try
        {
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            var query = _repository.GetAll(
                predicate: x => x.Status == (int)EntityStatus.Active &&
                                x.ParentId == null &&
                                x.OperationType == operationType,
                include: q => q.Include(e => e.Children),
                ignoreQueryFilters: true);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            var items = await query.ToListAsync();

            var mapped = items
                .Select(x => new ExpenseDefinitionListDto
                {
                    Id = x.Id,
                    OperationType = x.OperationType,
                    Name = x.Name,
                    ParentId = x.ParentId,
                    ParentName = null
                })
                .ToList();

            if (mapped?.Count > 0)
            {
                response.Result.Data = mapped.AsQueryable().OrderBy(x => x.Name);
            }

            response.Result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetMainExpenses Exception {Ex}", ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<List<ExpenseDefinitionListDto>>> GetMainExpenses(ExpenseOperationType operationType)
    {
        var rs = new IActionResult<List<ExpenseDefinitionListDto>> { Result = new List<ExpenseDefinitionListDto>() };
        try
        {
            if (!await CanView())
            {
                rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return rs;
            }

            var query = _repository.GetAll(
                predicate: x => x.Status == (int)EntityStatus.Active &&
                                x.ParentId == null &&
                                x.OperationType == operationType,
                ignoreQueryFilters: true);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            var items = await query.ToListAsync();

            rs.Result = items
                .OrderBy(x => x.Name)
                .Select(x => new ExpenseDefinitionListDto
                {
                    Id = x.Id,
                    OperationType = x.OperationType,
                    Name = x.Name
                })
                .ToList();

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetMainExpenses(list) Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<List<ExpenseDefinitionListDto>>> GetSubExpenses(int parentId)
    {
        var rs = new IActionResult<List<ExpenseDefinitionListDto>> { Result = new List<ExpenseDefinitionListDto>() };
        try
        {
            if (!await CanView())
            {
                rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return rs;
            }

            var query = _repository.GetAll(
                predicate: x => x.Status == (int)EntityStatus.Active &&
                                x.ParentId == parentId,
                include: q => q.Include(e => e.Parent),
                ignoreQueryFilters: true);

            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            var items = await query.ToListAsync();

            rs.Result = items
                .OrderBy(x => x.Name)
                .Select(x => new ExpenseDefinitionListDto
                {
                    Id = x.Id,
                    OperationType = x.OperationType,
                    Name = x.Name,
                    ParentId = x.ParentId,
                    ParentName = x.Parent != null ? x.Parent.Name : null
                })
                .ToList();

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetSubExpenses Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> UpsertExpense(AuditWrapDto<ExpenseDefinitionUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
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

                // Check for duplicate name in current branch
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(e => e.Name.ToLower() == dto.Name.ToLower() && e.BranchId == currentBranchId && e.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{dto.Name}' isimli gider tanımı bu şubede zaten mevcut.");
                    return rs;
                }
                
                var entity = new ExpenseDefinition
                {
                    OperationType = dto.OperationType,
                    Name = dto.Name,
                    ParentId = dto.ParentId,
                    BranchId = currentBranchId,
                    Status = (int)EntityStatus.Active,
                    CreatedId = model.UserId,
                    CreatedDate = DateTime.Now
                };

                await _repository.InsertAsync(entity);
            }
            else
            {
                if (!await CanEdit())
                {
                    rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                    return rs;
                }
                
                var query = _repository.GetAll(
                    predicate: x => x.Id == dto.Id,
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                    rs.AddError("Gider tanımı bulunamadı veya yetkiniz yok.");
                    return rs;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                     rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return rs;
                }

                // Check for duplicate name in same branch (excluding current entity)
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(e => e.Id != dto.Id && e.Name.ToLower() == dto.Name.ToLower() && e.BranchId == entity.BranchId && e.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{dto.Name}' isimli gider tanımı bu şubede zaten mevcut.");
                    return rs;
                }

                entity.OperationType = dto.OperationType;
                entity.Name = dto.Name;
                entity.ParentId = dto.ParentId;
                entity.ModifiedId = model.UserId;
                entity.ModifiedDate = DateTime.Now;
                _repository.Update(entity);
            }

            await _context.SaveChangesAsync();
            var result = _context.LastSaveChangesResult;
            if (result.IsOk)
            {
                rs.AddSuccess("Kayıt İşlemi Başarılı");
                return rs;
            }
            else
            {
                if (result != null && result.Exception != null)
                {
                    if (result.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        rs.AddError($"'{dto.Name}' isimli gider tanımı zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                    }
                    else
                    {
                        rs.AddError(result.Exception.ToString());
                    }
                }
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("UpsertExpense Exception " + ex.ToString());
            if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                rs.AddError($"'{model.Dto.Name}' isimli gider tanımı zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
            }
            else
            {
                rs.AddSystemError(ex.ToString());
            }
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> DeleteExpense(AuditWrapDto<ExpenseDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!await CanDelete())
            {
                rs.AddError("Silme yetkiniz bulunmamaktadır.");
                return rs;
            }

            var query = _repository.GetAll(
                predicate: f => f.Id == model.Dto.Id,
                ignoreQueryFilters: true);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            var entity = await query.FirstOrDefaultAsync();

            if (entity == null)
            {
                rs.AddError("Gider tanımı bulunamadı veya yetkiniz yok.");
                return rs;
            }

            if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
            {
                 rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                 return rs;
            }

            entity.Status = (int)EntityStatus.Deleted;
            entity.DeletedDate = DateTime.Now;
            entity.DeletedId = model.UserId;
            _repository.Update(entity);

            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Gider tanımı silindi.");
                return rs;
            }

            if (lastResult.Exception != null)
                rs.AddError(lastResult.Exception.ToString());

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteExpense Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<ExpenseDefinitionUpsertDto>> GetExpenseById(int id)
    {
        var rs = new IActionResult<ExpenseDefinitionUpsertDto> { Result = new ExpenseDefinitionUpsertDto() };
        try
        {
            if (!await CanView())
            {
                rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return rs;
            }

            var query = _repository.GetAll(
                predicate: x => x.Id == id && x.Status != (int)EntityStatus.Deleted,
                ignoreQueryFilters: true);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            var entity = await query.FirstOrDefaultAsync();
            if (entity == null)
                return rs;

            rs.Result = new ExpenseDefinitionUpsertDto
            {
                Id = entity.Id,
                OperationType = entity.OperationType,
                Name = entity.Name,
                ParentId = entity.ParentId
            };

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetExpenseById Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
}


