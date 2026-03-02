using ecommerce.Admin.Domain.Dtos.CheckDto;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Extensions;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate
{
    public class CheckService : ICheckService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private readonly ILogger<CheckService> _logger;
        private const string MENU_NAME = "checks";

        public CheckService(
            IUnitOfWork<ApplicationDbContext> context,
            ITenantProvider tenantProvider,
            ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService,
            ILogger<CheckService> logger)
        {
            _context = context;
            _tenantProvider = tenantProvider;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<IActionResult<Paging<List<CheckListDto>>>> GetPaged(
            PageSetting pager,
            int? bankId = null,
            int? customerId = null,
            CheckStatus? checkStatus = null,
            DateTime? dueDateStart = null,
            DateTime? dueDateEnd = null)
        {
            var result = new IActionResult<Paging<List<CheckListDto>>>
            {
                Result = new Paging<List<CheckListDto>> { Data = new List<CheckListDto>(), DataCount = 0 }
            };

            try
            {
                if (!await _permissionService.CanView(MENU_NAME))
                {
                    result.AddError("Bu sayfayı görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var query = _context.DbContext.Checks
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(x => x.Status == (int)EntityStatus.Active);

                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                if (bankId.HasValue && bankId.Value > 0)
                    query = query.Where(x => x.BankId == bankId.Value);
                if (customerId.HasValue && customerId.Value > 0)
                    query = query.Where(x => x.CustomerId == customerId.Value);
                if (checkStatus.HasValue)
                    query = query.Where(x => x.CheckStatus == checkStatus.Value);
                if (dueDateStart.HasValue)
                    query = query.Where(x => x.DueDate >= dueDateStart.Value);
                if (dueDateEnd.HasValue)
                    query = query.Where(x => x.DueDate <= dueDateEnd.Value.Date.AddDays(1).AddSeconds(-1));

                query = query
                    .Include(x => x.Customer)
                    .Include(x => x.Bank)
                    .Include(x => x.BankBranch).ThenInclude(b => b!.City)
                    .Include(x => x.BankBranch).ThenInclude(b => b!.Town)
                    .Include(x => x.Currency);

                var totalCount = await query.CountAsync();
                var skip = pager.Skip ?? 0;
                var take = Math.Min(pager.Take ?? 25, 500);

                var list = await query
                    .OrderByDescending(x => x.DueDate)
                    .ThenByDescending(x => x.Id)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var dtos = list.Select(x => new CheckListDto
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer?.Name ?? "",
                    BankId = x.BankId,
                    BankName = x.Bank?.Name ?? "",
                    BankBranchId = x.BankBranchId,
                    BankBranchName = x.BankBranch?.Name,
                    CityName = x.BankBranch?.City?.Name,
                    TownName = x.BankBranch?.Town?.Name,
                    CheckNumber = x.CheckNumber,
                    Amount = x.Amount,
                    CurrencyCode = x.Currency?.CurrencyCode ?? "",
                    DueDate = x.DueDate,
                    CheckStatus = x.CheckStatus,
                    CheckStatusName = x.CheckStatus.GetDisplayName(),
                    Description = x.Description,
                    BranchId = x.BranchId,
                    ReceivedDate = x.ReceivedDate,
                    SettlementDate = x.SettlementDate,
                    CreatedDate = x.CreatedDate
                }).ToList();

                result.Result = new Paging<List<CheckListDto>>
                {
                    Data = dtos,
                    DataCount = totalCount,
                    TotalRawCount = totalCount,
                    CurrentPage = take > 0 ? (skip / take) + 1 : 1,
                    PageSize = take
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPaged Checks");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<CheckUpsertDto>> GetById(int id)
        {
            var result = new IActionResult<CheckUpsertDto>();
            try
            {
                if (!await _permissionService.CanView(MENU_NAME))
                {
                    result.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var query = _context.DbContext.Checks
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == id);
                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                var entity = await query.FirstOrDefaultAsync();
                if (entity == null)
                {
                    result.AddError("Çek bulunamadı veya yetkiniz yok.");
                    return result;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                    result.AddError("Bu kayıt için şube yetkiniz bulunmamaktadır.");
                    return result;
                }

                result.Result = new CheckUpsertDto
                {
                    Id = entity.Id,
                    CustomerId = entity.CustomerId,
                    BankId = entity.BankId,
                    BankBranchId = entity.BankBranchId,
                    CheckNumber = entity.CheckNumber,
                    Amount = entity.Amount,
                    DueDate = entity.DueDate,
                    CurrencyId = entity.CurrencyId,
                    CheckStatus = entity.CheckStatus,
                    Description = entity.Description,
                    ReceivedDate = entity.ReceivedDate,
                    SettlementDate = entity.SettlementDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetById Check {Id}", id);
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<int>> Create(AuditWrapDto<CheckUpsertDto> model)
        {
            var result = new IActionResult<int> { Result = 0 };
            try
            {
                if (!await _permissionService.CanCreate(MENU_NAME))
                {
                    result.AddError("Ekleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var branchId = model.Dto.BranchId ?? _tenantProvider.GetCurrentBranchId();
                var checkNumber = string.IsNullOrWhiteSpace(model.Dto.CheckNumber)
                    ? await GetNextCheckNumberAsync(branchId)
                    : model.Dto.CheckNumber.Trim();

                var entity = new Check
                {
                    CustomerId = model.Dto.CustomerId,
                    BankId = model.Dto.BankId,
                    BankBranchId = model.Dto.BankBranchId,
                    CheckNumber = checkNumber,
                    Amount = model.Dto.Amount,
                    DueDate = model.Dto.DueDate,
                    CurrencyId = model.Dto.CurrencyId,
                    CheckStatus = model.Dto.CheckStatus,
                    Description = model.Dto.Description,
                    ReceivedDate = model.Dto.ReceivedDate,
                    SettlementDate = model.Dto.SettlementDate,
                    BranchId = branchId,
                    Status = (int)EntityStatus.Active,
                    CreatedDate = DateTime.Now,
                    CreatedId = model.UserId
                };

                var repo = _context.GetRepository<Check>();
                repo.Insert(entity);
                await _context.SaveChangesAsync();

                if (entity.CheckStatus == CheckStatus.InPortfolio)
                    await CreateCustomerAccountTransactionForCheckAsync(entity, CustomerAccountTransactionType.Credit, "Çek portföye alındı", model.UserId);

                result.Result = entity.Id;
                result.AddSuccess("Çek kaydı oluşturuldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create Check");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<Empty>> Update(AuditWrapDto<CheckUpsertDto> model)
        {
            var result = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await _permissionService.CanEdit(MENU_NAME))
                {
                    result.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var dto = model.Dto;
                if (!dto.Id.HasValue || dto.Id.Value == 0)
                {
                    result.AddError("Güncellenecek kayıt bulunamadı.");
                    return result;
                }

                var query = _context.DbContext.Checks
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == dto.Id.Value);
                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                var entity = await query.FirstOrDefaultAsync();
                if (entity == null)
                {
                    result.AddError("Çek bulunamadı veya yetkiniz yok.");
                    return result;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                    result.AddError("Bu kayıt için şube yetkiniz bulunmamaktadır.");
                    return result;
                }

                var previousStatus = entity.CheckStatus;
                entity.CustomerId = dto.CustomerId;
                entity.BankId = dto.BankId;
                entity.BankBranchId = dto.BankBranchId;
                entity.CheckNumber = string.IsNullOrWhiteSpace(dto.CheckNumber) ? entity.CheckNumber : dto.CheckNumber.Trim();
                entity.Amount = dto.Amount;
                entity.DueDate = dto.DueDate;
                entity.CurrencyId = dto.CurrencyId;
                entity.CheckStatus = dto.CheckStatus;
                entity.Description = dto.Description;
                entity.ReceivedDate = dto.ReceivedDate;
                entity.SettlementDate = dto.SettlementDate;
                entity.ModifiedDate = DateTime.Now;
                entity.ModifiedId = model.UserId;

                var repo = _context.GetRepository<Check>();
                repo.Update(entity);
                await _context.SaveChangesAsync();

                if (previousStatus == CheckStatus.InPortfolio && entity.CheckStatus != CheckStatus.InPortfolio)
                {
                    var description = entity.CheckStatus switch
                    {
                        CheckStatus.Collected => "Çek tahsil edildi",
                        CheckStatus.Bounced => "Çek reddedildi",
                        CheckStatus.Returned => "Çek iade edildi",
                        _ => "Çek durumu güncellendi"
                    };
                    await CreateCustomerAccountTransactionForCheckAsync(entity, CustomerAccountTransactionType.Debit, description, model.UserId);
                }

                result.AddSuccess("Çek kaydı güncellendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update Check");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<Empty>> Delete(AuditWrapDto<CheckDeleteDto> model)
        {
            var result = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await _permissionService.CanDelete(MENU_NAME))
                {
                    result.AddError("Silme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var query = _context.DbContext.Checks
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == model.Dto.Id);
                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                var entity = await query.FirstOrDefaultAsync();
                if (entity == null)
                {
                    result.AddError("Çek bulunamadı veya yetkiniz yok.");
                    return result;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                    result.AddError("Bu kayıt için şube yetkiniz bulunmamaktadır.");
                    return result;
                }

                var wasInPortfolio = entity.CheckStatus == CheckStatus.InPortfolio;

                entity.Status = (int)EntityStatus.Deleted;
                entity.DeletedDate = DateTime.Now;
                entity.DeletedId = model.UserId;

                var repo = _context.GetRepository<Check>();
                repo.Update(entity);
                await _context.SaveChangesAsync();

                if (wasInPortfolio)
                    await CreateCustomerAccountTransactionForCheckAsync(entity, CustomerAccountTransactionType.Debit, "Çek silindi (iptal)", model.UserId);

                result.AddSuccess("Çek kaydı silindi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete Check");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        private async Task<string> GetNextCheckNumberAsync(int branchId)
        {
            var today = DateTime.Now.Date;
            var count = await _context.DbContext.Checks
                .IgnoreQueryFilters()
                .Where(c => c.BranchId == branchId && c.CreatedDate >= today && c.CreatedDate < today.AddDays(1))
                .CountAsync();
            var seq = (count + 1).ToString("D4");
            return $"CHK-{today:yyyyMMdd}-{seq}";
        }

        private async Task CreateCustomerAccountTransactionForCheckAsync(Check check, CustomerAccountTransactionType transactionType, string descriptionPrefix, int userId)
        {
            var transactionRepo = _context.GetRepository<CustomerAccountTransaction>();
            var existing = await transactionRepo.GetAll(
                predicate: t => t.CustomerId == check.CustomerId && t.Status == (int)EntityStatus.Active,
                disableTracking: true
            ).ToListAsync();
            var currentBalance = existing
                .Where(t => t.TransactionType == CustomerAccountTransactionType.Debit)
                .Sum(t => t.Amount) -
                existing
                .Where(t => t.TransactionType == CustomerAccountTransactionType.Credit)
                .Sum(t => t.Amount);
            var balanceAfter = transactionType == CustomerAccountTransactionType.Debit
                ? currentBalance + check.Amount
                : currentBalance - check.Amount;
            var description = $"{descriptionPrefix} — #{check.CheckNumber}";
            var transaction = new CustomerAccountTransaction
            {
                CustomerId = check.CustomerId,
                CheckId = check.Id,
                TransactionType = transactionType,
                Amount = check.Amount,
                TransactionDate = DateTime.UtcNow,
                Description = description,
                ReferenceNo = check.CheckNumber,
                BalanceAfterTransaction = balanceAfter,
                BranchId = check.BranchId,
                Status = (int)EntityStatus.Active,
                CreatedDate = DateTime.UtcNow,
                CreatedId = userId
            };
            await transactionRepo.InsertAsync(transaction);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Çek {CheckId} için cari hareket — {Type}, Tutar: {Amount}", check.Id, transactionType, check.Amount);
        }
    }
}
