using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Extensions;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Admin.Services.Concreate
{
    public class CustomerAccountTransactionService : ICustomerAccountTransactionService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICollectionReceiptService _collectionReceiptService;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private const string MENU_NAME = "b2b/customer-account-report";
        /// <summary>Menü yolu: Tüm cari hareketler listesi (sadece B2B Admin / bu menüye yetkili roller)</summary>
        private const string ADMIN_TRANSACTIONS_MENU = "b2b/customer-account-transactions";

        public CustomerAccountTransactionService(
            IUnitOfWork<ApplicationDbContext> context,
            IMapper mapper,
            ILogger logger,
            IServiceScopeFactory scopeFactory,
            ITenantProvider tenantProvider,
            IHttpContextAccessor httpContextAccessor,
            ICollectionReceiptService collectionReceiptService,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService,
            ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter)
        {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
        _collectionReceiptService = collectionReceiptService;
        _permissionService = permissionService;
        _roleFilter = roleFilter;
    }

        private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

        // ... Existing GetCustomerAccountTransactions method ...

        public async Task<IActionResult<decimal>> GetCustomerBalance(int customerId)
        {
            var result = new IActionResult<decimal> { Result = 0 };

            var scopedContext = _context;
            var roleFilter = _roleFilter;

            try
            {
                // Allow B2B Customers to see their own balance even if they don't have menu permission
                if (!await CanView() && !(_tenantProvider.IsCustomerB2B && _tenantProvider.GetCustomerId() == customerId))
                {
                    result.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var transactionRepo = scopedContext.GetRepository<CustomerAccountTransaction>();
                
                var query = transactionRepo.GetAll(
                    predicate: t => t.CustomerId == customerId && t.Status == (int)EntityStatus.Active,
                    disableTracking: true,
                    ignoreQueryFilters: true
                ).AsQueryable();

                query = roleFilter.ApplyFilter(query, scopedContext.DbContext);

                var transactions = await query
                    .Include(t => t.Order)
                    .Where(t => t.Order == null || t.Order.OrderStatusType != OrderStatusType.OrderCanceled)
                    .ToListAsync();

                var totalDebit = transactions
                    .Where(t => t.TransactionType == CustomerAccountTransactionType.Debit)
                    .Sum(t => t.Amount);

                var totalCredit = transactions
                    .Where(t => t.TransactionType == CustomerAccountTransactionType.Credit)
                    .Sum(t => t.Amount);

                result.Result = totalDebit - totalCredit; // Borç - Alacak = Bakiye
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetCustomerBalance Exception: {ex}");
                result.AddSystemError(ex.ToString());
            }

            return result;
        }

        public async Task<IActionResult<List<CustomerAccountTransactionListDto>>> GetCustomerAccountTransactions(
            int customerId, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            var result = new IActionResult<List<CustomerAccountTransactionListDto>> 
            { 
                Result = new List<CustomerAccountTransactionListDto>() 
            };

            var scopedContext = _context;
            var roleFilter = _roleFilter;

            try
            {
                // Allow B2B Customers to see their own transactions even if they don't have menu permission
                if (!await CanView() && !(_tenantProvider.IsCustomerB2B && _tenantProvider.GetCustomerId() == customerId))
                {
                    result.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var transactionRepo = scopedContext.GetRepository<CustomerAccountTransaction>();
                
                var query = transactionRepo.GetAll(
                    predicate: t => t.CustomerId == customerId && t.Status == (int)EntityStatus.Active,
                    disableTracking: true,
                    ignoreQueryFilters: true
                ).AsQueryable();

                query = roleFilter.ApplyFilter(query, scopedContext.DbContext);

                query = query
                    .Include(t => t.Customer)
                    .Include(t => t.Order)
                    .Include(t => t.Invoice)
                    .Include(t => t.CashRegister)
                    .Where(t => t.Order == null || t.Order.OrderStatusType != OrderStatusType.OrderCanceled);

                // Tarih filtresi
                if (startDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(t => t.TransactionDate <= endDate.Value.AddDays(1).AddSeconds(-1));
                }

                var transactions = await query
                    .OrderByDescending(t => t.TransactionDate)
                    .ThenByDescending(t => t.Id)
                    .ToListAsync();

                var dtos = transactions.Select(t => new CustomerAccountTransactionListDto
                {
                    Id = t.Id,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer?.Name ?? string.Empty,
                    OrderId = t.OrderId,
                    OrderNumber = t.Order?.OrderNumber,
                    InvoiceId = t.InvoiceId,
                    InvoiceNo = t.Invoice?.InvoiceNo,
                    Ettn = t.Invoice?.Ettn,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.GetDisplayName(),
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate,
                    Description = t.Description,
                    PaymentTypeId = t.PaymentTypeId,
                    PaymentTypeName = t.PaymentTypeId?.GetDisplayName() ?? string.Empty,
                    CashRegisterId = t.CashRegisterId,
                    CashRegisterName = t.CashRegister?.Name,
                    ReferenceNo = t.ReferenceNo,
                    BalanceAfterTransaction = t.BalanceAfterTransaction,
                    // Yürüyen bakiye için giren/çıkan tutarları ayır
                    IncomingAmount = t.TransactionType == CustomerAccountTransactionType.Credit ? t.Amount : 0,
                    OutgoingAmount = t.TransactionType == CustomerAccountTransactionType.Debit ? t.Amount : 0
                }).ToList();

                // Faturaya bağlı TÜM siparişleri çek (birden fazla sipariş aynı faturaya bağlanabilir)
                var invoiceIds = dtos.Where(d => d.InvoiceId.HasValue).Select(d => d.InvoiceId!.Value).Distinct().ToList();
                if (invoiceIds.Any())
                {
                    var linkedOrders = await scopedContext.DbContext.Orders
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(o => o.InvoiceId.HasValue && invoiceIds.Contains(o.InvoiceId.Value) 
                            && o.Status != (int)EntityStatus.Deleted)
                        .Select(o => new { o.InvoiceId, o.Id, o.OrderNumber })
                        .ToListAsync();
                    
                    var ordersByInvoice = linkedOrders.GroupBy(o => o.InvoiceId!.Value)
                        .ToDictionary(g => g.Key, g => g.Select(o => new LinkedOrderDto 
                        { 
                            OrderId = o.Id, 
                            OrderNumber = o.OrderNumber 
                        }).ToList());
                    
                    foreach (var dto in dtos)
                    {
                        if (dto.InvoiceId.HasValue && ordersByInvoice.TryGetValue(dto.InvoiceId.Value, out var orders))
                        {
                            dto.LinkedOrders = orders;
                        }
                    }
                }

                result.Result = dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetCustomerAccountTransactions Exception: {ex}");
                result.AddSystemError(ex.ToString());
            }

            return result;
        }

        public async Task<IActionResult<CustomerAccountReportDto>> GetCustomerAccountReport(
            int customerId, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            var result = new IActionResult<CustomerAccountReportDto> 
            { 
                Result = new CustomerAccountReportDto() 
            };

            // using var scope = _scopeFactory.CreateScope();
            // var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var scopedContext = _context;

            try
            {
                // Müşteri bilgisini al
                var customerRepo = scopedContext.GetRepository<Customer>();
                var customer = await customerRepo.GetAll(
                    predicate: c => c.Id == customerId,
                    disableTracking: true
                ).FirstOrDefaultAsync();

                if (customer == null)
                {
                    result.AddError("Müşteri bulunamadı.");
                    return result;
                }

                // Hareketleri al
                var transactionsResult = await GetCustomerAccountTransactions(customerId, startDate, endDate);
                if (!transactionsResult.Ok)
                {
                    result.AddError(transactionsResult.Metadata?.Message ?? "Hareketler alınırken hata oluştu.");
                    return result;
                }

                // Bakiye hesapla
                // Not: GetCustomerBalance artık kendi içinde scope yönetiyor, güvenle çağrılabilir.
                var balanceResult = await GetCustomerBalance(customerId);
                if (!balanceResult.Ok)
                {
                    result.AddError(balanceResult.Metadata?.Message ?? "Bakiye hesaplanırken hata oluştu.");
                    return result;
                }

                // Toplam borç ve alacak hesapla
                var totalDebit = transactionsResult.Result
                    .Where(t => t.TransactionType == CustomerAccountTransactionType.Debit)
                    .Sum(t => t.Amount);

                var totalCredit = transactionsResult.Result
                    .Where(t => t.TransactionType == CustomerAccountTransactionType.Credit)
                    .Sum(t => t.Amount);

                result.Result = new CustomerAccountReportDto
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    CustomerCode = customer.Code,
                    CustomerEmail = customer.Email,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    Balance = balanceResult.Result,
                    Transactions = transactionsResult.Result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetCustomerAccountReport Exception: {ex}");
                result.AddSystemError(ex.ToString());
            }

            return result;
        }

        public async Task<IActionResult<int>> CreateTransaction(AuditWrapDto<CustomerAccountTransactionUpsertDto> model)
        {
            var result = new IActionResult<int> { Result = 0 };

            try
            {
                if (!await _permissionService.CanCreate(MENU_NAME))
                {
                   result.AddError("Ekleme yetkiniz bulunmamaktadır.");
                   return result;
                }

                // Mevcut bakiyeyi hesapla
                var balanceResult = await GetCustomerBalance(model.Dto.CustomerId);
                if (!balanceResult.Ok)
                {
                    result.AddError("Bakiye hesaplanırken hata oluştu.");
                    return result;
                }

                var currentBalance = balanceResult.Result;

                // Yeni bakiyeyi hesapla
                var balanceAfterTransaction = currentBalance;
                if (model.Dto.TransactionType == CustomerAccountTransactionType.Debit)
                {
                    balanceAfterTransaction += model.Dto.Amount;
                }
                else if (model.Dto.TransactionType == CustomerAccountTransactionType.Credit)
                {
                    balanceAfterTransaction -= model.Dto.Amount;
                }

                var entity = new CustomerAccountTransaction
                {
                    CustomerId = model.Dto.CustomerId,
                    OrderId = model.Dto.OrderId,
                    InvoiceId = model.Dto.InvoiceId,
                    TransactionType = model.Dto.TransactionType,
                    Amount = model.Dto.Amount,
                    TransactionDate = model.Dto.TransactionDate,
                    Description = model.Dto.Description,
                    PaymentTypeId = model.Dto.PaymentTypeId,
                    CashRegisterId = model.Dto.CashRegisterId,
                    ReferenceNo = model.Dto.ReferenceNo,
                    BalanceAfterTransaction = balanceAfterTransaction,
                    BranchId = _tenantProvider.GetCurrentBranchId(),
                    Status = (int)EntityStatus.Active,
                    CreatedDate = DateTime.Now,
                    CreatedId = model.UserId
                };

                var transactionRepo = _context.GetRepository<CustomerAccountTransaction>();
                transactionRepo.Insert(entity);
                await _context.SaveChangesAsync();

                result.Result = entity.Id;
                result.AddSuccess("Cari hesap hareketi oluşturuldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"CreateTransaction Exception: {ex}");
                result.AddSystemError(ex.ToString());
            }

            return result;
        }


        public async Task<IActionResult<CustomerAccountReportDto>> GetPlasiyerAccountSummary(int userId)
        {
            var result = new IActionResult<CustomerAccountReportDto> { Result = new CustomerAccountReportDto() };
            
            // using var scope = _scopeFactory.CreateScope();
            // var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            // var roleFilter = scope.ServiceProvider.GetRequiredService<ecommerce.Admin.Domain.Services.IRoleBasedFilterService>();
            var scopedContext = _context;
            var roleFilter = _roleFilter;

            try
            {
                if (!await CanView())
                {
                    result.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                // 1. Get Plasiyer
                var userRepo = scopedContext.GetRepository<ApplicationUser>();
                var appUser = await userRepo.GetFirstOrDefaultAsync(predicate: u => u.Id == userId);

                if (appUser == null || !appUser.SalesPersonId.HasValue) 
                    return result;

                // 2. Get Linked Customers
                var customerIds = await scopedContext.DbContext.CustomerPlasiyers
                    .AsNoTracking()
                    .Where(cp => cp.SalesPersonId == appUser.SalesPersonId.Value)
                    .Select(cp => cp.CustomerId)
                    .ToListAsync();

                if (!customerIds.Any()) return result;

                 // 3. Aggregate Balance
                 var transactionRepo = scopedContext.GetRepository<CustomerAccountTransaction>();
                 
                 var transactionsQuery = scopedContext.DbContext.CustomerAccountTransactions
                     .AsNoTracking()
                     .IgnoreQueryFilters()
                     .Where(t => customerIds.Contains(t.CustomerId) && t.Status == (int)EntityStatus.Active);
                 
                 transactionsQuery = roleFilter.ApplyFilter(transactionsQuery, scopedContext.DbContext);
                 
                 transactionsQuery = transactionsQuery
                     .Include(t => t.Order)
                     .Where(t => t.Order == null || t.Order.OrderStatusType != OrderStatusType.OrderCanceled);

                 var totalDebit = await transactionsQuery
                    .Where(t => t.TransactionType == CustomerAccountTransactionType.Debit)
                    .SumAsync(t => t.Amount);

                 var totalCredit = await transactionsQuery
                    .Where(t => t.TransactionType == CustomerAccountTransactionType.Credit)
                    .SumAsync(t => t.Amount);

                 result.Result.TotalDebit = totalDebit;
                 result.Result.TotalCredit = totalCredit;
                 result.Result.Balance = totalDebit - totalCredit;
            }
            catch (Exception ex)
            {
               _logger.LogError($"GetPlasiyerAccountSummary Exception: {ex}");
               result.AddSystemError(ex.Message);
            }
            return result;
        }

        public async Task<IActionResult<PaymentReceiptDto>> GetTransactionForReceipt(int transactionId, int customerId, int? salesPersonId = null)
        {
            var result = new IActionResult<PaymentReceiptDto>();

            try
            {
                var transactionRepo = _context.GetRepository<CustomerAccountTransaction>();
                var transaction = await transactionRepo.GetAll(
                    predicate: t => t.Id == transactionId && t.CustomerId == customerId && t.Status == (int)EntityStatus.Active,
                    disableTracking: true,
                    ignoreQueryFilters: true
                )
                .Include(t => t.Customer)
                .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    result.AddError("Hareket bulunamadı veya bu carie ait değil.");
                    return result;
                }

                if (transaction.TransactionType != CustomerAccountTransactionType.Credit)
                {
                    result.AddError("Sadece tahsilat (alacak) hareketleri için makbuz gönderilebilir.");
                    return result;
                }

                var desc = transaction.Description ?? string.Empty;
                if (!desc.Contains("tahsilat", StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError("Bu hareket tahsilat türünde değil.");
                    return result;
                }

                var customer = transaction.Customer;
                if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
                {
                    result.AddError("Caride e-posta adresi tanımlı değil. Makbuz gönderilemez.");
                    return result;
                }

                string? makbuzNo = null;
                if (salesPersonId.HasValue)
                {
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var userId = int.TryParse(userIdClaim, out var uid) ? uid : 0;
                    var branchId = _tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : (int?)null;
                    var receiptResult = await _collectionReceiptService.GetOrCreateMakbuzNoAsync(transactionId, customerId, salesPersonId.Value, branchId, userId);
                    if (receiptResult.Ok && !string.IsNullOrEmpty(receiptResult.Result))
                        makbuzNo = receiptResult.Result;
                }
                else
                {
                    makbuzNo = await _collectionReceiptService.GetMakbuzNoByTransactionIdAsync(transactionId);
                }

                result.Result = new PaymentReceiptDto
                {
                    MakbuzNo = makbuzNo,
                    CustomerEmail = customer.Email,
                    CustomerName = customer.Name ?? string.Empty,
                    CustomerCode = customer.Code ?? string.Empty,
                    TransactionDate = transaction.TransactionDate,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    PaymentTypeName = transaction.PaymentTypeId?.GetDisplayName()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTransactionForReceipt Exception. TransactionId: {TransactionId}, CustomerId: {CustomerId}", transactionId, customerId);
                result.AddSystemError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Tüm cari hareketleri sayfalı getirir. Tenant (ApplyFilter) ve isteğe bağlı müşteri/tarih filtreleri uygulanır.
        /// FilterCustomers = aynı filtreye göre hareketi olan cariler; CustomerSubtotals = bu sayfa verisinden cari bazlı alt toplamlar.
        /// </summary>
        public async Task<IActionResult<CustomerAccountTransactionsPageResult>> GetPagedAllCustomerAccountTransactions(
            PageSetting pager,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var result = new IActionResult<CustomerAccountTransactionsPageResult>
            {
                Result = new CustomerAccountTransactionsPageResult()
            };

            try
            {
                if (!await _permissionService.CanView(ADMIN_TRANSACTIONS_MENU))
                {
                    result.AddError("Bu sayfayı görüntüleme yetkiniz bulunmamaktadır.");
                    return result;
                }

                var scopedContext = _context;
                var roleFilter = _roleFilter;

                var query = scopedContext.DbContext.CustomerAccountTransactions
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(t => t.Status == (int)EntityStatus.Active);

                query = roleFilter.ApplyFilter(query, scopedContext.DbContext);

                if (customerId.HasValue && customerId.Value > 0)
                    query = query.Where(t => t.CustomerId == customerId.Value);

                if (startDate.HasValue)
                    query = query.Where(t => t.TransactionDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(t => t.TransactionDate <= endDate.Value.AddDays(1).AddSeconds(-1));

                query = query
                    .Include(t => t.Customer)
                    .Include(t => t.Order)
                    .Include(t => t.Invoice)
                    .Include(t => t.CashRegister)
                    .Where(t => t.Order == null || t.Order.OrderStatusType != OrderStatusType.OrderCanceled);

                var totalCount = await query.CountAsync();

                var skip = pager.Skip ?? 0;
                var take = Math.Min(pager.Take ?? 25, 500);

                var transactions = await query
                    .OrderByDescending(t => t.TransactionDate)
                    .ThenByDescending(t => t.Id)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var dtos = transactions.Select(t => new CustomerAccountTransactionListDto
                {
                    Id = t.Id,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer?.Name ?? string.Empty,
                    OrderId = t.OrderId,
                    OrderNumber = t.Order?.OrderNumber,
                    InvoiceId = t.InvoiceId,
                    InvoiceNo = t.Invoice?.InvoiceNo,
                    Ettn = t.Invoice?.Ettn,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.GetDisplayName(),
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate,
                    Description = t.Description,
                    PaymentTypeId = t.PaymentTypeId,
                    PaymentTypeName = t.PaymentTypeId?.GetDisplayName() ?? string.Empty,
                    CashRegisterId = t.CashRegisterId,
                    CashRegisterName = t.CashRegister?.Name,
                    ReferenceNo = t.ReferenceNo,
                    BalanceAfterTransaction = t.BalanceAfterTransaction,
                    IncomingAmount = t.TransactionType == CustomerAccountTransactionType.Credit ? t.Amount : 0,
                    OutgoingAmount = t.TransactionType == CustomerAccountTransactionType.Debit ? t.Amount : 0
                }).ToList();

                var invoiceIds = dtos.Where(d => d.InvoiceId.HasValue).Select(d => d.InvoiceId!.Value).Distinct().ToList();
                if (invoiceIds.Any())
                {
                    var linkedOrders = await scopedContext.DbContext.Orders
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(o => o.InvoiceId.HasValue && invoiceIds.Contains(o.InvoiceId.Value)
                            && o.Status != (int)EntityStatus.Deleted)
                        .Select(o => new { o.InvoiceId, o.Id, o.OrderNumber })
                        .ToListAsync();

                    var ordersByInvoice = linkedOrders.GroupBy(o => o.InvoiceId!.Value)
                        .ToDictionary(g => g.Key, g => g.Select(o => new LinkedOrderDto
                        {
                            OrderId = o.Id,
                            OrderNumber = o.OrderNumber
                        }).ToList());

                    foreach (var dto in dtos)
                    {
                        if (dto.InvoiceId.HasValue && ordersByInvoice.TryGetValue(dto.InvoiceId.Value, out var orders))
                            dto.LinkedOrders = orders;
                    }
                }

                var filterCustomers = scopedContext.DbContext.CustomerAccountTransactions
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(t => t.Status == (int)EntityStatus.Active)
                    .Where(t => t.Customer != null);
                filterCustomers = roleFilter.ApplyFilter(filterCustomers, scopedContext.DbContext);
                if (customerId.HasValue && customerId.Value > 0)
                    filterCustomers = filterCustomers.Where(t => t.CustomerId == customerId.Value);
                if (startDate.HasValue)
                    filterCustomers = filterCustomers.Where(t => t.TransactionDate >= startDate.Value);
                if (endDate.HasValue)
                    filterCustomers = filterCustomers.Where(t => t.TransactionDate <= endDate.Value.AddDays(1).AddSeconds(-1));
                var filterCustomerList = await filterCustomers
                    .Select(t => new { t.CustomerId, t.Customer!.Name })
                    .Distinct()
                    .OrderBy(x => x.Name)
                    .Select(x => new FilterCustomerItemDto { Id = x.CustomerId, DisplayName = x.Name ?? "" })
                    .ToListAsync();

                var customerSubtotals = dtos
                    .GroupBy(t => new { t.CustomerId, t.CustomerName })
                    .Select(g => new CustomerSubtotalItemDto
                    {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = g.Key.CustomerName,
                        Debit = g.Sum(t => t.OutgoingAmount),
                        Credit = g.Sum(t => t.IncomingAmount),
                        Net = g.Sum(t => t.OutgoingAmount) - g.Sum(t => t.IncomingAmount)
                    })
                    .ToList();

                result.Result = new CustomerAccountTransactionsPageResult
                {
                    Data = dtos,
                    DataCount = totalCount,
                    TotalRawCount = totalCount,
                    CurrentPage = take > 0 ? (skip / take) + 1 : 1,
                    PageSize = take,
                    FilterCustomers = filterCustomerList,
                    CustomerSubtotals = customerSubtotals
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPagedAllCustomerAccountTransactions Exception");
                result.AddSystemError(ex.ToString());
            }

            return result;
        }
    }
}
