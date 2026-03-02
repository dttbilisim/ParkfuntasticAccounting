using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Services.Concreate;

/// <summary>
/// Tahsilat makbuzu servisi — plasiyer bazlı otomatik makbuz numarası ve tabloda saklama.
/// </summary>
public class CollectionReceiptService : ICollectionReceiptService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ITenantProvider _tenantProvider;

    public CollectionReceiptService(
        IUnitOfWork<ApplicationDbContext> context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<string> GetNextMakbuzNoAsync(int salesPersonId)
    {
        var year = DateTime.Now.Year;
        var repo = _context.GetRepository<CollectionReceipt>();
        var count = await repo.GetAll(
                predicate: r => r.SalesPersonId == salesPersonId
                    && r.CreatedDate.Year == year
                    && r.Status == (int)EntityStatus.Active,
                disableTracking: true,
                ignoreQueryFilters: true)
            .CountAsync();
        var seq = count + 1;
        return $"TM-{salesPersonId}-{year}-{seq:D5}";
    }

    /// <inheritdoc />
    public async Task<IActionResult<string>> GetOrCreateMakbuzNoAsync(int customerAccountTransactionId, int customerId, int salesPersonId, int? branchId, int userId)
    {
        var result = new IActionResult<string>();
        try
        {
            var repo = _context.GetRepository<CollectionReceipt>();
            var existing = await repo.GetAll(
                    predicate: r => r.CustomerAccountTransactionId == customerAccountTransactionId
                        && r.Status == (int)EntityStatus.Active,
                    disableTracking: true,
                    ignoreQueryFilters: true)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                result.Result = existing.MakbuzNo;
                return result;
            }

            return await CreateReceiptAsync(customerAccountTransactionId, customerId, salesPersonId, branchId, userId);
        }
        catch (Exception ex)
        {
            result.AddSystemError(ex.Message);
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<IActionResult<string>> CreateReceiptAsync(int customerAccountTransactionId, int customerId, int salesPersonId, int? branchId, int userId)
    {
        var result = new IActionResult<string>();
        try
        {
            var repo = _context.GetRepository<CollectionReceipt>();
            var existing = await repo.GetAll(
                    predicate: r => r.CustomerAccountTransactionId == customerAccountTransactionId
                        && r.Status == (int)EntityStatus.Active,
                    disableTracking: true,
                    ignoreQueryFilters: true)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                result.Result = existing.MakbuzNo;
                return result;
            }

            var makbuzNo = await GetNextMakbuzNoAsync(salesPersonId);
            var branch = branchId ?? (_tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : 0);

            var entity = new CollectionReceipt
            {
                MakbuzNo = makbuzNo,
                CustomerId = customerId,
                SalesPersonId = salesPersonId,
                CustomerAccountTransactionId = customerAccountTransactionId,
                BranchId = branch > 0 ? branch : null,
                Status = (int)EntityStatus.Active,
                CreatedDate = DateTime.Now,
                CreatedId = userId
            };

            repo.Insert(entity);
            await _context.SaveChangesAsync();

            result.Result = makbuzNo;
            return result;
        }
        catch (Exception ex)
        {
            result.AddSystemError(ex.Message);
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetMakbuzNoByTransactionIdAsync(int customerAccountTransactionId)
    {
        var repo = _context.GetRepository<CollectionReceipt>();
        var receipt = await repo.GetAll(
                predicate: r => r.CustomerAccountTransactionId == customerAccountTransactionId
                    && r.Status == (int)EntityStatus.Active,
                disableTracking: true,
                ignoreQueryFilters: true)
            .Select(r => r.MakbuzNo)
            .FirstOrDefaultAsync();
        return receipt;
    }
}
