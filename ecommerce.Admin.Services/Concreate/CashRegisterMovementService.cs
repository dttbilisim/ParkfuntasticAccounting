using ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto;
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
    public class CashRegisterMovementService : ICashRegisterMovementService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private readonly ILogger<CashRegisterMovementService> _logger;
        private const string MENU_NAME = "cash-register-movements";

        public CashRegisterMovementService(
            IUnitOfWork<ApplicationDbContext> context,
            ITenantProvider tenantProvider,
            ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService,
            ILogger<CashRegisterMovementService> logger)
        {
            _context = context;
            _tenantProvider = tenantProvider;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<IActionResult<Paging<List<CashRegisterMovementListDto>>>> GetPaged(
            PageSetting pager,
            int? cashRegisterId = null,
            CashRegisterMovementType? movementType = null,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var result = new IActionResult<Paging<List<CashRegisterMovementListDto>>>
            {
                Result = new Paging<List<CashRegisterMovementListDto>> { Data = new List<CashRegisterMovementListDto>(), DataCount = 0 }
            };

            try
            {
                if (!await _permissionService.CanView(MENU_NAME))
                {
                    result.AddError("Bu sayfayı görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var query = _context.DbContext.CashRegisterMovements
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(x => x.Status == (int)EntityStatus.Active);

                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                if (cashRegisterId.HasValue && cashRegisterId.Value > 0)
                    query = query.Where(x => x.CashRegisterId == cashRegisterId.Value);
                if (movementType.HasValue)
                    query = query.Where(x => x.MovementType == movementType.Value);
                if (customerId.HasValue && customerId.Value > 0)
                    query = query.Where(x => x.CustomerId == customerId.Value);
                if (startDate.HasValue)
                    query = query.Where(x => x.TransactionDate >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(x => x.TransactionDate <= endDate.Value.AddDays(1).AddSeconds(-1));

                query = query
                    .Include(x => x.CashRegister)
                    .Include(x => x.Customer)
                    .Include(x => x.PaymentType)
                    .Include(x => x.Currency);

                var totalCount = await query.CountAsync();
                var skip = pager.Skip ?? 0;
                var take = Math.Min(pager.Take ?? 25, 500);

                var list = await query
                    .OrderByDescending(x => x.TransactionDate)
                    .ThenByDescending(x => x.Id)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var dtos = list.Select(x => new CashRegisterMovementListDto
                {
                    Id = x.Id,
                    CashRegisterId = x.CashRegisterId,
                    CashRegisterName = x.CashRegister?.Name ?? "",
                    MovementType = x.MovementType,
                    MovementTypeName = x.MovementType.GetDisplayName(),
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer?.Name,
                    PaymentTypeId = x.PaymentTypeId,
                    PaymentTypeName = x.PaymentType?.Name,
                    CurrencyId = x.CurrencyId,
                    CurrencyCode = x.Currency?.CurrencyCode ?? "",
                    Amount = x.Amount,
                    TransactionDate = x.TransactionDate,
                    Description = x.Description
                }).ToList();

                result.Result = new Paging<List<CashRegisterMovementListDto>>
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
                _logger.LogError(ex, "GetPaged CashRegisterMovements");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<CashRegisterMovementUpsertDto>> GetById(int id)
        {
            var result = new IActionResult<CashRegisterMovementUpsertDto>();
            try
            {
                if (!await _permissionService.CanView(MENU_NAME))
                {
                    result.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var query = _context.DbContext.CashRegisterMovements
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == id);
                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                var entity = await query.FirstOrDefaultAsync();
                if (entity == null)
                {
                    result.AddError("Kasa hareketi bulunamadı veya yetkiniz yok.");
                    return result;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                    result.AddError("Bu kayıt için şube yetkiniz bulunmamaktadır.");
                    return result;
                }

                result.Result = new CashRegisterMovementUpsertDto
                {
                    Id = entity.Id,
                    CashRegisterId = entity.CashRegisterId,
                    MovementType = entity.MovementType,
                    CustomerId = entity.CustomerId,
                    PaymentTypeId = entity.PaymentTypeId,
                    CurrencyId = entity.CurrencyId,
                    Amount = entity.Amount,
                    TransactionDate = entity.TransactionDate,
                    Description = entity.Description
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetById CashRegisterMovement {Id}", id);
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<int>> Create(AuditWrapDto<CashRegisterMovementUpsertDto> model)
        {
            var result = new IActionResult<int> { Result = 0 };
            try
            {
                if (!await _permissionService.CanCreate(MENU_NAME))
                {
                    result.AddError("Ekleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var branchId = _tenantProvider.GetCurrentBranchId();
                var entity = new CashRegisterMovement
                {
                    CashRegisterId = model.Dto.CashRegisterId,
                    MovementType = model.Dto.MovementType,
                    CustomerId = model.Dto.CustomerId,
                    PaymentTypeId = model.Dto.PaymentTypeId,
                    CurrencyId = model.Dto.CurrencyId,
                    Amount = model.Dto.Amount,
                    TransactionDate = model.Dto.TransactionDate,
                    Description = model.Dto.Description,
                    BranchId = branchId,
                    Status = (int)EntityStatus.Active,
                    CreatedDate = DateTime.Now,
                    CreatedId = model.UserId
                };

                var repo = _context.GetRepository<CashRegisterMovement>();
                repo.Insert(entity);
                await _context.SaveChangesAsync();

                result.Result = entity.Id;
                result.AddSuccess("Kasa hareketi oluşturuldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create CashRegisterMovement");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<Empty>> Update(AuditWrapDto<CashRegisterMovementUpsertDto> model)
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

                var query = _context.DbContext.CashRegisterMovements
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == dto.Id.Value);
                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                var entity = await query.FirstOrDefaultAsync();
                if (entity == null)
                {
                    result.AddError("Kasa hareketi bulunamadı veya yetkiniz yok.");
                    return result;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                    result.AddError("Bu kayıt için şube yetkiniz bulunmamaktadır.");
                    return result;
                }

                entity.CashRegisterId = dto.CashRegisterId;
                entity.MovementType = dto.MovementType;
                entity.CustomerId = dto.CustomerId;
                entity.PaymentTypeId = dto.PaymentTypeId;
                entity.CurrencyId = dto.CurrencyId;
                entity.Amount = dto.Amount;
                entity.TransactionDate = dto.TransactionDate;
                entity.Description = dto.Description;
                entity.ModifiedDate = DateTime.Now;
                entity.ModifiedId = model.UserId;

                var repo = _context.GetRepository<CashRegisterMovement>();
                repo.Update(entity);
                await _context.SaveChangesAsync();

                result.AddSuccess("Kasa hareketi güncellendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update CashRegisterMovement");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<Empty>> Delete(AuditWrapDto<CashRegisterMovementDeleteDto> model)
        {
            var result = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await _permissionService.CanDelete(MENU_NAME))
                {
                    result.AddError("Silme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var query = _context.DbContext.CashRegisterMovements
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == model.Dto.Id);
                query = _roleFilter.ApplyFilter(query, _context.DbContext);

                var entity = await query.FirstOrDefaultAsync();
                if (entity == null)
                {
                    result.AddError("Kasa hareketi bulunamadı veya yetkiniz yok.");
                    return result;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId, _context.DbContext))
                {
                    result.AddError("Bu kayıt için şube yetkiniz bulunmamaktadır.");
                    return result;
                }

                entity.Status = (int)EntityStatus.Deleted;
                entity.DeletedDate = DateTime.Now;
                entity.DeletedId = model.UserId;

                var repo = _context.GetRepository<CashRegisterMovement>();
                repo.Update(entity);
                await _context.SaveChangesAsync();

                result.AddSuccess("Kasa hareketi silindi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete CashRegisterMovement");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<Empty>> CreateTransfer(AuditWrapDto<CashRegisterTransferDto> model)
        {
            var result = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await _permissionService.CanCreate(MENU_NAME))
                {
                    result.AddError("Virman işlemi için yetkiniz bulunmamaktadır.");
                    return result;
                }

                var dto = model.Dto;
                if (dto.SourceCashRegisterId == dto.TargetCashRegisterId)
                {
                    result.AddError("Kaynak ve hedef kasa aynı olamaz.");
                    return result;
                }

                IQueryable<CashRegister> crQuery = _context.DbContext.CashRegisters
                    .IgnoreQueryFilters()
                    .Where(x => x.Status != (int)EntityStatus.Deleted)
                    .Include(x => x.Currency);

                crQuery = _roleFilter.ApplyFilter(crQuery, _context.DbContext);

                var sourceCr = await crQuery.FirstOrDefaultAsync(x => x.Id == dto.SourceCashRegisterId);
                var targetCr = await crQuery.FirstOrDefaultAsync(x => x.Id == dto.TargetCashRegisterId);

                if (sourceCr == null || targetCr == null)
                {
                    result.AddError("Kaynak veya hedef kasa bulunamadı veya yetkiniz yok.");
                    return result;
                }

                if (sourceCr.CurrencyId != dto.CurrencyId || targetCr.CurrencyId != dto.CurrencyId)
                {
                    result.AddError("Virman için her iki kasanın da aynı dövizde olması gerekir.");
                    return result;
                }

                var branchId = _tenantProvider.GetCurrentBranchId();
                var descOut = "Virman → " + targetCr.Name;
                var descIn = "Virman ← " + sourceCr.Name;
                if (!string.IsNullOrWhiteSpace(dto.Description))
                {
                    descOut += " — " + dto.Description.Trim();
                    descIn += " — " + dto.Description.Trim();
                }

                var outMovement = new CashRegisterMovement
                {
                    CashRegisterId = dto.SourceCashRegisterId,
                    MovementType = CashRegisterMovementType.Out,
                    CustomerId = null,
                    PaymentTypeId = null,
                    CurrencyId = dto.CurrencyId,
                    Amount = dto.Amount,
                    TransactionDate = dto.TransactionDate,
                    Description = descOut,
                    BranchId = branchId,
                    Status = (int)EntityStatus.Active,
                    CreatedDate = DateTime.Now,
                    CreatedId = model.UserId
                };

                var inMovement = new CashRegisterMovement
                {
                    CashRegisterId = dto.TargetCashRegisterId,
                    MovementType = CashRegisterMovementType.In,
                    CustomerId = null,
                    PaymentTypeId = null,
                    CurrencyId = dto.CurrencyId,
                    Amount = dto.Amount,
                    TransactionDate = dto.TransactionDate,
                    Description = descIn,
                    BranchId = branchId,
                    Status = (int)EntityStatus.Active,
                    CreatedDate = DateTime.Now,
                    CreatedId = model.UserId
                };

                var repo = _context.GetRepository<CashRegisterMovement>();
                repo.Insert(outMovement);
                repo.Insert(inMovement);
                await _context.SaveChangesAsync();

                result.AddSuccess("Kasalar arası virman işlemi oluşturuldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateTransfer");
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        public async Task<IActionResult<List<CashRegisterBalanceSummaryDto>>> GetBalanceSummary(
            int? cashRegisterId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var result = new IActionResult<List<CashRegisterBalanceSummaryDto>>
            {
                Result = new List<CashRegisterBalanceSummaryDto>()
            };

            try
            {
                if (!await _permissionService.CanView(MENU_NAME))
                {
                    result.AddError("Bu sayfayı görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                IQueryable<CashRegister> crQuery = _context.DbContext.CashRegisters
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(x => x.Status != (int)EntityStatus.Deleted)
                    .Include(x => x.Currency);

                crQuery = _roleFilter.ApplyFilter(crQuery, _context.DbContext);

                if (cashRegisterId.HasValue && cashRegisterId.Value > 0)
                    crQuery = crQuery.Where(x => x.Id == cashRegisterId.Value);

                var cashRegisters = await crQuery.ToListAsync();
                if (!cashRegisters.Any())
                    return result;

                IQueryable<CashRegisterMovement> movementQuery = _context.DbContext.CashRegisterMovements
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(x => x.Status == (int)EntityStatus.Active);

                movementQuery = _roleFilter.ApplyFilter(movementQuery, _context.DbContext);

                if (cashRegisterId.HasValue && cashRegisterId.Value > 0)
                    movementQuery = movementQuery.Where(x => x.CashRegisterId == cashRegisterId.Value);
                if (startDate.HasValue)
                    movementQuery = movementQuery.Where(x => x.TransactionDate >= startDate.Value);
                if (endDate.HasValue)
                    movementQuery = movementQuery.Where(x => x.TransactionDate <= endDate.Value.AddDays(1).AddSeconds(-1));

                var summaries = new List<CashRegisterBalanceSummaryDto>();

                foreach (var cr in cashRegisters)
                {
                    var inOutQuery = movementQuery.Where(x => x.CashRegisterId == cr.Id && x.CurrencyId == cr.CurrencyId);
                    var totalIn = await inOutQuery.Where(x => x.MovementType == CashRegisterMovementType.In).SumAsync(x => x.Amount);
                    var totalOut = await inOutQuery.Where(x => x.MovementType == CashRegisterMovementType.Out).SumAsync(x => x.Amount);

                    summaries.Add(new CashRegisterBalanceSummaryDto
                    {
                        CashRegisterId = cr.Id,
                        CashRegisterName = cr.Name ?? "",
                        CurrencyCode = cr.Currency?.CurrencyCode ?? "TRY",
                        OpeningBalance = cr.OpeningBalance,
                        TotalIn = totalIn,
                        TotalOut = totalOut
                    });
                }

                result.Result = summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBalanceSummary");
                result.AddSystemError(ex.Message);
            }

            return result;
        }
    }
}
