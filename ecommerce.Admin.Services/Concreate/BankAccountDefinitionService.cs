using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Extensions;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountExpenseDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountInstallmentDto;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Utils;
using Npgsql;

namespace ecommerce.Admin.Services.Concreate;

public class BankAccountDefinitionService : IBankAccountDefinitionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "bank-accounts";

    public BankAccountDefinitionService(
        IUnitOfWork<ApplicationDbContext> unitOfWork,
        ITenantProvider tenantProvider,
        IHttpContextAccessor httpContextAccessor,
        ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _unitOfWork = unitOfWork;
        _dbContext = unitOfWork.DbContext;
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
        _roleFilter = roleFilter;
        _permissionService = permissionService;
    }

    private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
    private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
    private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
    private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

        public async Task<IActionResult<Paging<List<BankAccountListDto>>>> GetBankAccounts(PageSetting pager)
    {
        var response = OperationResult.CreateResult<Paging<List<BankAccountListDto>>>();
        try
        {
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            var queryBase = _dbContext.BankAccounts.AsQueryable();
            queryBase = _roleFilter.ApplyFilter(queryBase, _dbContext);

            var query =
                from ba in queryBase
                join b in _dbContext.Banks on ba.BankId equals b.Id into bankJoin
                from b in bankJoin.DefaultIfEmpty()
                join c in _dbContext.Currencies on ba.CurrencyId equals c.Id into currencyJoin
                from c in currencyJoin.DefaultIfEmpty()
                where ba.Status != (int)EntityStatus.Deleted
                select new
                {
                    Id = ba.Id,
                    BankId = ba.BankId,
                    BankName = ba.BankName,
                    BankNameFromJoin = b != null ? b.Name : null,
                    SystemCode = ba.SystemCode,
                    PaymentType = ba.PaymentType,
                    CurrencyId = ba.CurrencyId,
                    CurrencyName = c != null ? c.CurrencyName : null,
                    AccountCode = ba.AccountCode,
                    AccountName = ba.AccountName,
                    City = ba.City,
                    BranchName = ba.BranchName,
                    Iban = ba.Iban,
                    Active = ba.Active
                };

            var pagedResult = await query.ToPagedResultAsync(pager);
            
            var result = pagedResult.Data.Select(x => new BankAccountListDto
            {
                Id = x.Id,
                BankId = x.BankId,
                BankName = !string.IsNullOrEmpty(x.BankName) ? x.BankName : x.BankNameFromJoin,
                SystemCode = x.SystemCode,
                PaymentType = x.PaymentType.GetDisplayName(),
                CurrencyId = x.CurrencyId,
                CurrencyName = x.CurrencyName,
                AccountCode = x.AccountCode,
                AccountName = x.AccountName,
                City = x.City,
                BranchName = x.BranchName,
                Iban = x.Iban,
                Active = x.Active
            }).ToList();

            response.Result = new Paging<List<BankAccountListDto>>
            {
                Data = result,
                DataCount = pagedResult.DataCount
            };
        }
        catch (Exception ex)
        {
            response.AddSystemError(ex.ToString());
        }

        return response;
    }

    public async Task<IActionResult<BankAccountUpsertDto>> GetBankAccountById(int id)
    {
        var rs = new IActionResult<BankAccountUpsertDto> { Result = new BankAccountUpsertDto() };
        try
        {
            if (id == 0)
            {
                return rs;
            }

            if (!await CanView())
            {
                rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return rs;
            }

            var query = _dbContext.BankAccounts
                .IgnoreQueryFilters()
                .Where(x => x.Id == id);
            
            query = _roleFilter.ApplyFilter(query, _dbContext);

            var entity = await query.FirstOrDefaultAsync();

            if (entity == null)
            {
                var exists = await _dbContext.BankAccounts.IgnoreQueryFilters().AnyAsync(x => x.Id == id);
                if (exists)
                {
                     rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return rs;
                }
                rs.AddError("Kayıt bulunamadı.");
                return rs;
            }

            if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _dbContext))
            {
                 rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                 return rs;
            }

            rs.Result = new BankAccountUpsertDto
            {
                Id = entity.Id,
                BankId = entity.BankId,
                SystemCode = entity.SystemCode,
                PaymentType = entity.PaymentType,
                CurrencyId = entity.CurrencyId,
                AccountCode = entity.AccountCode,
                AccountName = entity.AccountName,
                City = entity.City,
                BankName = entity.BankName,
                BranchName = entity.BranchName,
                CardNumber = entity.CardNumber,
                Iban = entity.Iban,
                Description = entity.Description,
                Active = entity.Active
            };

            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> UpsertBankAccount(AuditWrapDto<BankAccountUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            var dto = model.Dto;
            if (dto.Id == 0)
            {
                if (!await CanCreate())
                {
                    rs.AddError("Ekleme yetkiniz bulunmamaktadır.");
                    return rs;
                }

                // Check for duplicate account name in current branch
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var duplicate = await _dbContext.BankAccounts.IgnoreQueryFilters()
                    .AnyAsync(ba => ba.AccountName.ToLower() == dto.AccountName.ToLower() && ba.BranchId == currentBranchId && ba.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{dto.AccountName}' isimli banka hesabı bu şubede zaten mevcut.");
                    return rs;
                }

                var entity = new BankAccount
                {
                    BankId = dto.BankId,
                    SystemCode = dto.SystemCode,
                    PaymentType = dto.PaymentType,
                    CurrencyId = dto.CurrencyId,
                    AccountCode = dto.AccountCode,
                    AccountName = dto.AccountName,
                    City = dto.City,
                    BankName = dto.BankName,
                    BranchName = dto.BranchName,
                    CardNumber = dto.CardNumber,
                    Iban = dto.Iban,
                    Description = dto.Description,
                    Active = dto.Active,
                    BranchId = currentBranchId
                };

                await _dbContext.BankAccounts.AddAsync(entity);
            }
            else
            {
                if (!await CanEdit())
                {
                    rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                    return rs;
                }

                var query = _dbContext.BankAccounts
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == dto.Id);
                
                query = _roleFilter.ApplyFilter(query, _dbContext);

                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                    rs.AddError("Kayıt bulunamadı veya yetkiniz yok.");
                    return rs;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _dbContext))
                {
                     rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return rs;
                }

                // Check for duplicate account name in same branch (excluding current entity)
                var duplicate = await _dbContext.BankAccounts.IgnoreQueryFilters()
                    .AnyAsync(ba => ba.Id != dto.Id && ba.AccountName.ToLower() == dto.AccountName.ToLower() && ba.BranchId == entity.BranchId && ba.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{dto.AccountName}' isimli banka hesabı bu şubede zaten mevcut.");
                    return rs;
                }

                entity.BankId = dto.BankId;
                entity.SystemCode = dto.SystemCode;
                entity.PaymentType = dto.PaymentType;
                entity.CurrencyId = dto.CurrencyId;
                entity.AccountCode = dto.AccountCode;
                entity.AccountName = dto.AccountName;
                entity.City = dto.City;
                entity.BankName = dto.BankName;
                entity.BranchName = dto.BranchName;
                entity.CardNumber = dto.CardNumber;
                entity.Iban = dto.Iban;
                entity.Description = dto.Description;
                entity.Active = dto.Active;
            }

            await _unitOfWork.SaveChangesAsync();
            var lastResult = _unitOfWork.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Kayıt kaydedildi.");
                return rs;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null)
                {
                    if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        rs.AddError($"'{dto.AccountName}' isimli banka hesabı zaten mevcut.");
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
            if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                rs.AddError($"'{model.Dto.AccountName}' isimli banka hesabı zaten mevcut.");
            }
            else
            {
                rs.AddSystemError(ex.ToString());
            }
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> DeleteBankAccount(AuditWrapDto<BankAccountDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!await CanDelete())
            {
                rs.AddError("Silme yetkiniz bulunmamaktadır.");
                return rs;
            }
            var id = model.Dto.Id;

            var query = _dbContext.BankAccounts
                .IgnoreQueryFilters()
                .Where(x => x.Id == id);
            
            query = _roleFilter.ApplyFilter(query, _dbContext);

            var entity = await query.FirstOrDefaultAsync();

            if (entity == null)
            {
                rs.AddError("Kayıt bulunamadı veya yetkiniz yok.");
                return rs;
            }

            if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _dbContext))
            {
                 rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                 return rs;
            }

            _dbContext.BankAccounts.Remove(entity);
            await _unitOfWork.SaveChangesAsync();
            rs.AddSuccess("Kayıt silindi.");
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<List<BankAccountExpenseListDto>>> GetBankAccountExpenses(int bankAccountId)
    {
        var rs = new IActionResult<List<BankAccountExpenseListDto>> { Result = new List<BankAccountExpenseListDto>() };
        try
        {
            if (!await CanView())
            {
                rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return rs;
            }
            var bankAccountQuery = _dbContext.BankAccounts.IgnoreQueryFilters().Where(x => x.Id == bankAccountId);
            bankAccountQuery = _roleFilter.ApplyFilter(bankAccountQuery, _dbContext);
            if (!await bankAccountQuery.AnyAsync())
            {
                rs.AddError("Banka hesabı bulunamadı veya yetkiniz yok.");
                return rs;
            }

            var list =
                await (from e in _dbContext.BankAccountExpenses
                       join me in _dbContext.ExpenseDefinitions on e.MainExpenseId equals me.Id
                       join se in _dbContext.ExpenseDefinitions on e.SubExpenseId equals se.Id into subJoin
                       from se in subJoin.DefaultIfEmpty()
                       where e.BankAccountId == bankAccountId
                       select new BankAccountExpenseListDto
                       {
                           Id = e.Id,
                           BankAccountId = e.BankAccountId,
                           MainExpenseId = e.MainExpenseId,
                           MainExpenseName = me.Name,
                           SubExpenseId = e.SubExpenseId,
                           SubExpenseName = se != null ? se.Name : null
                       }).ToListAsync();

            rs.Result = list;
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> UpsertBankAccountExpense(AuditWrapDto<BankAccountExpenseUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!await CanEdit())
            {
                rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                return rs;
            }
            var dto = model.Dto;
            
            // Verify BankAccountId access
            var bankAccountId = dto.Id == 0 ? dto.BankAccountId : 0;
            if (dto.Id != 0)
            {
                 var existing = await _dbContext.BankAccountExpenses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.Id);
                 if (existing != null) bankAccountId = existing.BankAccountId;
            }
            
            if (bankAccountId > 0)
            {
                var bankAccountQuery = _dbContext.BankAccounts.IgnoreQueryFilters().Where(x => x.Id == bankAccountId);
                bankAccountQuery = _roleFilter.ApplyFilter(bankAccountQuery, _dbContext);
                if (!await bankAccountQuery.AnyAsync())
                {
                    rs.AddError("Banka hesabı bulunamadı veya yetkiniz yok.");
                    return rs;
                }
            }

            if (dto.Id == 0)
            {
                var entity = new BankAccountExpense
                {
                    BankAccountId = dto.BankAccountId,
                    MainExpenseId = dto.MainExpenseId,
                    SubExpenseId = dto.SubExpenseId
                };

                await _dbContext.BankAccountExpenses.AddAsync(entity);
            }
            else
            {
                var entity = await _dbContext.BankAccountExpenses.FirstOrDefaultAsync(x => x.Id == dto.Id);
                if (entity == null)
                {
                    rs.AddError("Kayıt bulunamadı.");
                    return rs;
                }

                entity.MainExpenseId = dto.MainExpenseId;
                entity.SubExpenseId = dto.SubExpenseId;
            }

            await _unitOfWork.SaveChangesAsync();
            rs.AddSuccess("Kayıt kaydedildi.");
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> DeleteBankAccountExpense(AuditWrapDto<BankAccountExpenseDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!await CanDelete())
            {
                rs.AddError("Silme yetkiniz bulunmamaktadır.");
                return rs;
            }
            var id = model.Dto.Id;
            var entity = await _dbContext.BankAccountExpenses.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                rs.AddError("Kayıt bulunamadı.");
                return rs;
            }

            // Verify access
            var bankAccountQuery = _dbContext.BankAccounts.IgnoreQueryFilters().Where(x => x.Id == entity.BankAccountId);
            bankAccountQuery = _roleFilter.ApplyFilter(bankAccountQuery, _dbContext);
            if (!await bankAccountQuery.AnyAsync())
            {
                rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                return rs;
            }

            _dbContext.BankAccountExpenses.Remove(entity);
            await _unitOfWork.SaveChangesAsync();
            rs.AddSuccess("Kayıt silindi.");
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<List<BankAccountInstallmentListDto>>> GetBankAccountInstallments(int bankAccountId)
    {
        var rs = new IActionResult<List<BankAccountInstallmentListDto>> { Result = new List<BankAccountInstallmentListDto>() };
        try
        {
            if (!await CanView())
            {
                rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return rs;
            }
            var bankAccountQuery = _dbContext.BankAccounts.IgnoreQueryFilters().Where(x => x.Id == bankAccountId);
            bankAccountQuery = _roleFilter.ApplyFilter(bankAccountQuery, _dbContext);
            if (!await bankAccountQuery.AnyAsync())
            {
                rs.AddError("Banka hesabı bulunamadı veya yetkiniz yok.");
                return rs;
            }

            var list = await _dbContext.BankAccountInstallments
                .Where(x => x.BankAccountId == bankAccountId)
                .Select(x => new BankAccountInstallmentListDto
                {
                    Id = x.Id,
                    BankAccountId = x.BankAccountId,
                    Installment = x.Installment,
                    CommissionRate = x.CommissionRate,
                    Amount = x.Amount,
                    Note = x.Note,
                    Active = x.Active
                }).ToListAsync();

            rs.Result = list;
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> UpsertBankAccountInstallment(AuditWrapDto<BankAccountInstallmentUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!await CanEdit())
            {
                rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                return rs;
            }
            var dto = model.Dto;
            
             // Verify BankAccountId access
            var bankAccountId = dto.Id == 0 ? dto.BankAccountId : 0;
            if (dto.Id != 0)
            {
                 var existing = await _dbContext.BankAccountInstallments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.Id);
                 if (existing != null) bankAccountId = existing.BankAccountId;
            }
            
            if (bankAccountId > 0)
            {
                var bankAccountQuery = _dbContext.BankAccounts.IgnoreQueryFilters().Where(x => x.Id == bankAccountId);
                bankAccountQuery = _roleFilter.ApplyFilter(bankAccountQuery, _dbContext);
                if (!await bankAccountQuery.AnyAsync())
                {
                    rs.AddError("Banka hesabı bulunamadı veya yetkiniz yok.");
                    return rs;
                }
            }

            if (dto.Id == 0)
            {
                var entity = new BankAccountInstallment
                {
                    BankAccountId = dto.BankAccountId,
                    Installment = dto.Installment,
                    CommissionRate = dto.CommissionRate,
                    Amount = dto.Amount,
                    Note = dto.Note,
                    Active = dto.Active
                };

                await _dbContext.BankAccountInstallments.AddAsync(entity);
            }
            else
            {
                var entity = await _dbContext.BankAccountInstallments.FirstOrDefaultAsync(x => x.Id == dto.Id);
                if (entity == null)
                {
                    rs.AddError("Kayıt bulunamadı.");
                    return rs;
                }

                entity.Installment = dto.Installment;
                entity.CommissionRate = dto.CommissionRate;
                entity.Amount = dto.Amount;
                entity.Note = dto.Note;
                entity.Active = dto.Active;
            }

            await _unitOfWork.SaveChangesAsync();
            rs.AddSuccess("Kayıt kaydedildi.");
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> DeleteBankAccountInstallment(AuditWrapDto<BankAccountInstallmentDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            if (!await CanDelete())
            {
                rs.AddError("Silme yetkiniz bulunmamaktadır.");
                return rs;
            }
            var id = model.Dto.Id;
            var entity = await _dbContext.BankAccountInstallments.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                rs.AddError("Kayıt bulunamadı.");
                return rs;
            }
            
            // Verify access
            var bankAccountQuery = _dbContext.BankAccounts.IgnoreQueryFilters().Where(x => x.Id == entity.BankAccountId);
            bankAccountQuery = _roleFilter.ApplyFilter(bankAccountQuery, _dbContext);
            if (!await bankAccountQuery.AnyAsync())
            {
                rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                return rs;
            }

            _dbContext.BankAccountInstallments.Remove(entity);
            await _unitOfWork.SaveChangesAsync();
            rs.AddSuccess("Kayıt silindi.");
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
}


