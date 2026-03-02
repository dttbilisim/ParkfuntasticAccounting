using System.Globalization;
using AutoMapper;
using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Dtos.DashboardDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Mng.Jobs;
using ecommerce.Cargo.Sendeo.Jobs;
using ecommerce.Cargo.Yurtici.Jobs;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Hierarchical;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Extensions;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using ecommerce.Iyzico.Payment.Interface;
using Iyzipay.Model;
using Iyzipay.Request;
using System.Security.Claims;
using ecommerce.Core.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Entities.Accounting;
namespace ecommerce.Admin.Domain.Concreate{
    public class OrderService : IOrderService{
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Orders> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<OrderListDto> _radzenPagerService;
        private readonly IEmailService _emailService;
        private readonly IPaymentService _paymentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHangfireJobManager _hangfireJobManager;
        private readonly ecommerce.Admin.Domain.Interfaces.IRealTimeStockResolver _stockResolver;
        private static readonly System.Threading.SemaphoreSlim _dbSemaphore = new(1, 1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantProvider _tenantProvider;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "orders";

        public OrderService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<OrderListDto> radzenPagerService, IEmailService emailService, IPaymentService paymentService, IHttpContextAccessor httpContextAccessor,IHangfireJobManager hangfireJobManager, ecommerce.Admin.Domain.Interfaces.IRealTimeStockResolver stockResolver, IServiceScopeFactory scopeFactory, ITenantProvider tenantProvider, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService){
            _context = context;
            _repository = context.GetRepository<Orders>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _emailService = emailService;
            _paymentService = paymentService;
            _httpContextAccessor = httpContextAccessor;
            _hangfireJobManager = hangfireJobManager;
            _stockResolver = stockResolver;
            _scopeFactory = scopeFactory;
            _tenantProvider = tenantProvider;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }


        public async Task<IActionResult<Empty>> DeleteOrder(AuditWrapDto<OrderDeleteDto> model){
            var rs = new IActionResult<Empty>{Result = new Empty()};
            try{
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;
                
                var order = await _context.DbContext.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == model.Dto.Id);
                if (order == null)
                {
                    rs.AddError("Sipariş Bulunamadı");
                    return rs;
                }

                if (!isGlobalAdmin)
                {
                    var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         var isAllowed = await _context.DbContext.UserBranches
                            .AnyAsync(ub => ub.UserId == userId && ub.BranchId == order.BranchId && ub.Status == (int)EntityStatus.Active);
                         if (!isAllowed)
                         {
                             rs.AddError("Bu siparişi silme yetkiniz yok.");
                             return rs;
                         }
                    }
                }

                var strategy = _context.DbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var dbTransaction = await _context.BeginTransactionAsync();
                    try
                    {
                        _logger.LogInformation("DeleteOrder transaction started - OrderId: {OrderId}, UserId: {UserId}", model.Dto.Id, model.UserId);
                        
                        order.Status = (int)EntityStatus.Deleted;
                        order.DeletedDate = DateTime.Now;
                        order.DeletedId = model.UserId;

                        await _context.SaveChangesAsync();
                        var lastResult = _context.LastSaveChangesResult;
                        
                        if(lastResult.IsOk){
                            await dbTransaction.CommitAsync();
                            _logger.LogInformation("DeleteOrder transaction committed successfully - OrderId: {OrderId}", model.Dto.Id);
                            rs.AddSuccess("Successfull");
                        } else{
                            await dbTransaction.RollbackAsync();
                            _logger.LogError("DeleteOrder transaction rolled back - OrderId: {OrderId}, Error: {Error}", model.Dto.Id, lastResult?.Exception?.Message);
                            if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        await dbTransaction.RollbackAsync();
                        _logger.LogError(ex, "DeleteOrder transaction rolled back due to exception - OrderId: {OrderId}", model.Dto.Id);
                        throw;
                    }
                });
                return rs;
            } catch(Exception ex){
                _logger.LogError(ex, "DeleteOrder Exception - OrderId: {OrderId}", model.Dto.Id);
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<OrderUpsertDto>> GetOrderById(int Id){
            var rs = new IActionResult<OrderUpsertDto>{Result = new()};
            try{
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;

                var data = await _repository.GetFirstOrDefaultAsync(
                    predicate: f => f.Id == Id 
                        && (isGlobalAdmin ? (currentBranchId == 0 || f.BranchId == currentBranchId) : true),
                    ignoreQueryFilters: true);

                if (data != null && !isGlobalAdmin)
                {
                    var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         var isAllowed = await _context.DbContext.UserBranches
                            .AnyAsync(ub => ub.UserId == userId && ub.BranchId == data.BranchId && ub.Status == (int)EntityStatus.Active);
                         if (!isAllowed)
                         {
                             rs.AddError("Bu siparişi görme yetkiniz yok.");
                             return rs;
                         }
                         
                         if (currentBranchId > 0 && data.BranchId != currentBranchId)
                         {
                             rs.AddError("Sipariş seçili şubeye ait değil.");
                             return rs;
                         }
                    }
                }

                var mappedCat = _mapper.Map<OrderUpsertDto>(data);
                if(mappedCat != null){
                    rs.Result = mappedCat;
                } else
                    rs.AddError("Sipariş Bulunamadı");
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetOrderById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<List<KeyValuePair<string, int>>> OrderStatus(){
            await _dbSemaphore.WaitAsync();
            try
            {
                var ret = new List<KeyValuePair<string, int>>();
                
                // Use a new scope to avoid connection conflicts
                using (var scope = _scopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                    
                    var isGlobalAdmin = tenantProvider.IsGlobalAdmin;
                    var user = _httpContextAccessor.HttpContext?.User;
                    var currentBranchId = tenantProvider.GetCurrentBranchId();
                    
                    List<int> allowedBranchIds = new();
                    if (!isGlobalAdmin && user != null)
                    {
                         var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                         if (int.TryParse(userIdClaim, out int userId))
                         {
                             allowedBranchIds = await uow.DbContext.UserBranches
                                 .AsNoTracking()
                                 .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                                 .Select(ub => ub.BranchId)
                                 .ToListAsync();
                         }
                    }

                    var orderStatus = await uow.DbContext.Orders
                        .AsNoTracking()
                        .Where(x => x.Status != 99 && 
                             (isGlobalAdmin ? (currentBranchId == 0 || x.BranchId == currentBranchId) :
                             (allowedBranchIds.Contains(x.BranchId ?? 0) && (currentBranchId == 0 || x.BranchId == currentBranchId))))
                        .GroupBy(x => x.OrderStatusType)
                        .Select(x => new { Status = x.Key, Count = x.Count() })
                        .ToListAsync();
                    
                    foreach(var item in orderStatus){
                        ret.Add(new KeyValuePair<string, int>((item.Status.GetDisplayName() + " " + item.Count.ToString()), (int) item.Status));
                    }
                }
                
                return ret;
            }
            catch (Exception)
            {
                return new List<KeyValuePair<string, int>>();
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        public async Task<IActionResult<OrderListDto>> GetOrderDetailById(int id){
            IActionResult<OrderListDto> response = new(){Result = new()};
            try{
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;

                var data = await _context.DbContext.Orders
                    .IgnoreQueryFilters()
                    .AsSplitQuery()
                    .Include(x => x.OrderItems).ThenInclude(p => p.Product).ThenInclude(x => x.ProductImage)
                    .Include(x => x.OrderItems).ThenInclude(p => p.Product).ThenInclude(p => p.Tax)
                    .Include(x => x.OrderItems).ThenInclude(p => p.Product).ThenInclude(p => p.ProductUnits)
                    .Include(x => x.OrderItems).ThenInclude(oi => oi.AppliedDiscounts).ThenInclude(ad => ad.Discount)
                    .Include(x => x.Seller).ThenInclude(s => s.City)
                    .Include(x => x.Seller).ThenInclude(s => s.Town)
                    .Include(x => x.ApplicationUser)
                    .Include(x => x.UserAddress).ThenInclude(ua => ua.City)
                    .Include(x => x.UserAddress).ThenInclude(ua => ua.Town)
                    .Include(x => x.Cargo)
                    .Include(x => x.Bank)
                    .Include(x => x.Invoice)
                    .FirstOrDefaultAsync(x => x.Id == id 
                        && (isGlobalAdmin ? (currentBranchId == 0 || x.BranchId == currentBranchId) : true));

                if (data != null && !isGlobalAdmin)
                {
                    var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         var isAllowed = await _context.DbContext.UserBranches
                            .AnyAsync(ub => ub.UserId == userId && ub.BranchId == data.BranchId && ub.Status == (int)EntityStatus.Active);
                         if (!isAllowed)
                         {
                             response.AddError("Bu siparişi görme yetkiniz yok.");
                             return response;
                         }
                    }
                }
                var mappedCats = _mapper.Map<OrderListDto>(data);
                if(mappedCats != null){
                    // MANUAL FIX: Ensure CustomerName is set if AutoMapper failed
                    if (string.IsNullOrWhiteSpace(mappedCats.CustomerName))
                    {
                        mappedCats.CustomerName = !string.IsNullOrWhiteSpace(data.UserAddress?.FullName) ? data.UserAddress.FullName :
                                     (data.ApplicationUser != null ? (data.ApplicationUser.FirstName + " " + data.ApplicationUser.LastName).Trim() : "");
                    }
                    // MANUAL FIX: Ensure InvoiceId and InvoiceNo are set
                    mappedCats.InvoiceId = data.InvoiceId;
                    mappedCats.InvoiceNo = data.Invoice?.InvoiceNo;
                    // MANUAL FIX: Set CustomerId from ApplicationUser for invoice creation
                    mappedCats.CustomerId = data.ApplicationUser?.CustomerId;
                    
                    // Siparişe bağlı faturaları çek (LinkedInvoices)
                    // İki yönlü ilişki: Invoice.OrderId → siparişe bağlı fatura, Orders.InvoiceId → faturaya bağlı sipariş
                    var linkedInvoices = await _context.DbContext.Set<ecommerce.Core.Entities.Accounting.Invoice>()
                        .AsNoTracking()
                        .Where(inv => inv.Status != (int)EntityStatus.Deleted &&
                            (inv.OrderId == id || (data.InvoiceId.HasValue && inv.Id == data.InvoiceId.Value)))
                        .Select(inv => new { inv.Id, inv.InvoiceNo, inv.Ettn, inv.IsEInvoice, inv.IsEArchive, inv.EInvoiceStatus, inv.InvoiceDate, inv.TotalAmount, inv.DiscountTotal, inv.VatTotal, inv.GeneralTotal })
                        .Distinct()
                        .ToListAsync();
                    
                    if (linkedInvoices.Any())
                    {
                        mappedCats.LinkedInvoices = linkedInvoices.Select(inv => new OrderLinkedInvoiceDto
                        {
                            InvoiceId = inv.Id,
                            InvoiceNo = inv.InvoiceNo,
                            Ettn = inv.Ettn,
                            IsEInvoice = inv.IsEInvoice,
                            IsEArchive = inv.IsEArchive,
                            EInvoiceStatus = inv.EInvoiceStatus,
                            InvoiceDate = inv.InvoiceDate,
                            TotalAmount = inv.TotalAmount,
                            DiscountTotal = inv.DiscountTotal,
                            VatTotal = inv.VatTotal,
                            GeneralTotal = inv.GeneralTotal
                        }).ToList();
                    }
                    
                    response.Result = mappedCats;
                }
                return response;
            } catch(Exception ex){
                _logger.LogError("GetOrders Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<Empty>> UpdateOrderStatus(AuditWrapDto<OrderStatusUpdateDto> model){
            var response = OperationResult.CreateResult<Empty>();
            try{
                // Fix: Use a new scope to avoid tracking issues with existing context
                using (var scope = _scopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                    var hangfireJobManager = scope.ServiceProvider.GetRequiredService<IHangfireJobManager>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    var user = _httpContextAccessor.HttpContext?.User;
                    var repo = uow.GetRepository<Orders>();

                    var order = await repo.GetFirstOrDefaultAsync(
                        predicate:f => f.Id == model.Dto.Id, 
                        include:q => q.Include(o => o.ApplicationUser).Include(o => o.Seller).Include(o => o.Cargo!).Include(o => o.OrderItems), 
                        disableTracking:false,
                        ignoreQueryFilters: true);
                    
                    if(order == null || (isGlobalAdmin ? (currentBranchId != 0 && order.BranchId != currentBranchId) : false)){
                        response.AddError("Sipariş Bulunamadı");
                        return response;
                    }

                    if (!isGlobalAdmin)
                    {
                         var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                         if (int.TryParse(userIdClaim, out int userId))
                         {
                             var isAllowed = await uow.DbContext.UserBranches
                                .AnyAsync(ub => ub.UserId == userId && ub.BranchId == order.BranchId && ub.Status == (int)EntityStatus.Active);
                             if (!isAllowed)
                             {
                                 response.AddError("Bu siparişi güncelleme yetkiniz yok.");
                                 return response;
                             }
                         }
                    }
                    
                    var strategy = uow.DbContext.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        using var dbTransaction = await uow.BeginTransactionAsync();
                        try
                        {
                            _logger.LogInformation("UpdateOrderStatus transaction started - OrderId: {OrderId}, NewStatus: {NewStatus}", model.Dto.Id, model.Dto.OrderStatusType);
                            
                            order.OrderStatusType = model.Dto.OrderStatusType;
                            await uow.SaveChangesAsync();
                            if(!uow.LastSaveChangesResult.IsOk){
                                await dbTransaction.RollbackAsync();
                                _logger.LogError("UpdateOrderStatus transaction rolled back - OrderId: {OrderId}, Error: {Error}", model.Dto.Id, uow.LastSaveChangesResult.Exception?.Message);
                                throw uow.LastSaveChangesResult.Exception!;
                            }
                            
                            if(order.OrderStatusType == OrderStatusType.OrderCanceled){
                                // Use _httpContextAccessor from outer scope (injected) as it accesses current request context
                                var cancelPaymentStatus = await paymentService.PaymentCancel(new CreateCancelRequest{
                                        PaymentId = order.PaymentId, Ip = _httpContextAccessor.HttpContext.Connection?.RemoteIpAddress.ToString(), Locale = Locale.TR.ToString(), ConversationId = Guid.NewGuid().ToString(),
                                    }
                                );
                                if(cancelPaymentStatus.Status != "success"){
                                    await dbTransaction.RollbackAsync();
                                    _logger.LogError("UpdateOrderStatus transaction rolled back - Payment cancel failed - OrderId: {OrderId}, Error: {Error}", model.Dto.Id, cancelPaymentStatus.ErrorMessage);
                                    response.AddError("Ödeme sisteminde iptal edilemedi hata kodu:" + cancelPaymentStatus.ErrorMessage);
                                    return;
                                } else{
                                    if (order.Cargo != null && order.Cargo!.Name.ToLower().Contains("mng"))
                                    {
                                       //kargolar
                                        await hangfireJobManager.EnqueueAsync<MngOrderCancelJob>(new MngOrderCancelJobArgs { OrderId = order.Id });
                                    }
                                    else if (order.Cargo != null && order.Cargo!.Name.ToLower().Contains("sendeo"))
                                        {
                                            await hangfireJobManager.EnqueueAsync<SendeoOrderCancelJob>(new SendeoOrderCancelJobArgs { OrderId = order.Id });
                                        }
                                        else if (order.Cargo != null && order.Cargo!.Name.ToLower(new CultureInfo("tr-TR")).Contains("yurtiçi"))
                                        {
                                            await hangfireJobManager.EnqueueAsync<YurticiOrderCancelJob>(new YurticiOrderCancelJobArgs { OrderId = order.Id });
                                        }
                                    order.IyzicoCancelDate = DateTime.Now;
                                    order.IyzicoCanceledMessage = "Admin tarafından ödeme iptal edildi." +" Sipariş No:"+ order.OrderNumber;
                                    foreach (var oi in order.OrderItems ?? new List<OrderItems>())
                                        oi.CargoRequestHandled = false;
                                     await uow.SaveChangesAsync();
                                    
                                    if(!uow.LastSaveChangesResult.IsOk){
                                        await dbTransaction.RollbackAsync();
                                        _logger.LogError("UpdateOrderStatus transaction rolled back - Second SaveChanges failed - OrderId: {OrderId}, Error: {Error}", model.Dto.Id, uow.LastSaveChangesResult.Exception?.Message);
                                        throw uow.LastSaveChangesResult.Exception!;
                                    }
                                    
                                    // Sipariş iptal edildiğinde cari hesaba dokunma
                                    // Cari hesap borcu sadece fatura kesildiğinde oluşturulur, fatura iptalinde geri alınır
                                    
                                    await dbTransaction.CommitAsync();
                                    _logger.LogInformation("UpdateOrderStatus transaction committed successfully - OrderId: {OrderId}", model.Dto.Id);
                                    
                                    // Email gönderimi transaction dışında (non-critical)
                                    try
                                    {
                                        await emailService.SendOrderCancelledCustomerEmail(order);
                                    }
                                    catch (Exception emailEx)
                                    {
                                        _logger.LogWarning(emailEx, "Failed to send cancellation email - OrderId: {OrderId}", model.Dto.Id);
                                    }
                                }
                            }
                            else
                            {
                                await dbTransaction.CommitAsync();
                                _logger.LogInformation("UpdateOrderStatus transaction committed successfully - OrderId: {OrderId}", model.Dto.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            await dbTransaction.RollbackAsync();
                            _logger.LogError(ex, "UpdateOrderStatus transaction rolled back due to exception - OrderId: {OrderId}", model.Dto.Id);
                            throw;
                        }
                    });
                }
            } catch(Exception ex){
                _logger.LogError(ex, "UpdateOrderStatus Exception - OrderId: {OrderId}", model.Dto.Id);
                response.AddSystemError(ex.ToString());
            }
            return response;
        }

        public async Task<IActionResult<List<OrderInvoiceListDto>>> GetOrderInvoiceList(int orderId){
            IActionResult<List<OrderInvoiceListDto>> response = new(){Result = new List<OrderInvoiceListDto>()};
            try{
                var data = await _context.DbContext.OrderInvoices.Include(x => x.Orders).Include(x => x.Company).Where(x => x.OrderId == orderId).ToListAsync();
                var mapped = _mapper.Map<List<OrderInvoiceListDto>>(data);
                if(mapped != null){
                    response.Result = mapped;
                }
                return response;
            } catch(Exception ex){
                _logger.LogError("GetOrderInvoiceList Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<bool> UpsertOrderInvoice(OrderInvoiceUpsertDto input){
            var rs = new IActionResult<bool>{Result = new bool()};
            try{
                var strategy = _context.DbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var dbTransaction = await _context.BeginTransactionAsync();
                    try
                    {
                        _logger.LogInformation("UpsertOrderInvoice transaction started - OrderId: {OrderId}", input.OrderId);
                        
                        var dto = input;
                        var entity = _mapper.Map<OrderInvoice>(dto);
                        entity.Status = 1;
                        entity.CreatedId = 1;
                        entity.CreatedDate = DateTime.Now;
                        await _context.DbContext.OrderInvoices.AddAsync(entity);
                        await _context.SaveChangesAsync();
                        var lastResult = _context.LastSaveChangesResult;
                        
                        if(lastResult.IsOk){
                            await dbTransaction.CommitAsync();
                            _logger.LogInformation("UpsertOrderInvoice transaction committed successfully - InvoiceId: {InvoiceId}, OrderId: {OrderId}", entity.Id, input.OrderId);
                            rs.AddSuccess("ok");
                            rs.Result = true;
                        } else{
                            await dbTransaction.RollbackAsync();
                            _logger.LogError("UpsertOrderInvoice transaction rolled back - OrderId: {OrderId}, Error: {Error}", input.OrderId, lastResult?.Exception?.Message);
                            if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                            rs.Result = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        await dbTransaction.RollbackAsync();
                        _logger.LogError(ex, "UpsertOrderInvoice transaction rolled back due to exception - OrderId: {OrderId}", input.OrderId);
                        throw;
                    }
                });
                return rs.Result;
            } catch(Exception ex){
                _logger.LogError(ex, "UpsertOrderInvoice Exception - OrderId: {OrderId}", input.OrderId);
                rs.AddSystemError(ex.ToString());
                rs.Result = false;
                return rs.Result;
            }
        }
        public async Task<bool> DeleteOrderInvoice(int Id){
            var rs = new IActionResult<bool>{Result = new bool()};
            try{
                var strategy = _context.DbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var dbTransaction = await _context.BeginTransactionAsync();
                    try
                    {
                        _logger.LogInformation("DeleteOrderInvoice transaction started - InvoiceId: {InvoiceId}", Id);
                        
                        var row = await _context.DbContext.OrderInvoices.FirstOrDefaultAsync(x => x.Id == Id);
                        if (row == null)
                        {
                            await dbTransaction.RollbackAsync();
                            _logger.LogWarning("DeleteOrderInvoice transaction rolled back - Invoice not found - InvoiceId: {InvoiceId}", Id);
                            rs.AddError("Fatura bulunamadı");
                            rs.Result = false;
                            return;
                        }
                        
                        _context.DbContext.OrderInvoices.Remove(row);
                        await _context.SaveChangesAsync();
                        var lastResult = _context.LastSaveChangesResult;
                        
                        if(lastResult.IsOk){
                            await dbTransaction.CommitAsync();
                            _logger.LogInformation("DeleteOrderInvoice transaction committed successfully - InvoiceId: {InvoiceId}", Id);
                            rs.AddSuccess("ok");
                            rs.Result = true;
                        } else{
                            await dbTransaction.RollbackAsync();
                            _logger.LogError("DeleteOrderInvoice transaction rolled back - InvoiceId: {InvoiceId}, Error: {Error}", Id, lastResult?.Exception?.Message);
                            if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                            rs.Result = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        await dbTransaction.RollbackAsync();
                        _logger.LogError(ex, "DeleteOrderInvoice transaction rolled back due to exception - InvoiceId: {InvoiceId}", Id);
                        throw;
                    }
                });
                return rs.Result;
            } catch(Exception ex){
                _logger.LogError(ex, "DeleteOrderInvoice Exception - InvoiceId: {InvoiceId}", Id);
                rs.AddSystemError(ex.ToString());
                rs.Result = false;
                return rs.Result;
            }
        }
        public async Task<IActionResult<Paging<IQueryable<OrderListDto>>>> GetOrders(PageSetting pager){
            IActionResult<Paging<IQueryable<OrderListDto>>> response = new(){Result = new()};
            try{
                // Query oluştur
                var ordersQuery = _repository.GetAll(
                    predicate: f => f.Id != 0 && f.Status == (int) EntityStatus.Active,
                    include: x => x.Include(f => f.OrderItems)
                                   .ThenInclude(p => p.Product)
                                   .Include(x => x.Seller)
                                   .Include(x => x.ApplicationUser)
                                   .Include(f => f.UserAddress).ThenInclude(ua => ua!.City)
                                   .Include(f => f.UserAddress).ThenInclude(ua => ua!.Town)
                                   .Include(x => x.Bank)
                                   .Include(x => x.Invoice!),
                    ignoreQueryFilters: true
                );

                // Role-based filtering - clean ve maintainable
                ordersQuery = ApplyOrderRoleFilter(ordersQuery, _context.DbContext);
                
                // CRITICAL: ToList() to materialize
                var ordersList = ordersQuery.ToList();

                // Fetch Creator Names for Plasiyers (CreatedId != CompanyId)
                var plasiyerIds = ordersList
                    .Where(o => o.CreatedId != o.CompanyId)
                    .Select(o => o.CreatedId)
                    .Distinct()
                    .ToList();

                Dictionary<int, string> plasiyerNames = new();
                if (plasiyerIds.Any())
                {
                    // Use a new scope/context to avoid threading issues if reusing context, 
                    // but here we are in the same method. accessing _context directly is fine 
                    // as long as we don't have open DataReaders (we realized usages above with ToList)
                    // But accessing AspNetUsers might need check if it shares connection.
                    // Safe approach:
                     plasiyerNames = _context.DbContext.AspNetUsers
                        .AsNoTracking()
                        .Where(u => plasiyerIds.Contains(u.Id))
                        .Select(u => new { u.Id, Name = (u.FirstName + " " + u.LastName).Trim() })
                        .ToDictionary(k => k.Id, v => v.Name);
                }
                
                // MANUAL MAPPING to ensure User is not lost
                var mappedCats = ordersList.Select(order => 
                {
                    var isPlasiyer = order.CreatedId != order.CompanyId;
                    var customerName = !string.IsNullOrWhiteSpace(order.UserAddress?.FullName) 
                        ? order.UserAddress!.FullName 
                        : (order.UserFullName ?? "");

                    return new OrderListDto
                    {
                        Id = order.Id,
                        OrderNumber = order.OrderNumber,
                        OrderStatusType = order.OrderStatusType,
                        PlatformType = order.PlatformType, // Map PlatformType for filtering
                        PaymentTypeId = order.PaymentTypeId,
                        CreatedDate = order.CreatedDate,
                        ShipmentDate = order.OrderItems?.FirstOrDefault()?.ShipmentDate ?? order.CreatedDate,
                        CargoPrice = order.CargoPrice,
                        DiscountTotal = order.DiscountTotal,
                        ProductTotal = order.ProductTotal,
                        OrderTotal = order.OrderTotal,
                        GrandTotal = order.GrandTotal,
                        PaymentStatus = order.PaymentStatus,
                        IyzicoCancelDate = order.IyzicoCancelDate,
                        IyzicoCanceledMessage = order.IyzicoCanceledMessage,
                        IyzicoPaidTotal = order.IyzicoPaidTotal,
                        CompanyId = order.CompanyId, // FK: Works for both User.Id and ApplicationUser.Id
                        InvoiceId = order.InvoiceId,
                        InvoiceNo = order.Invoice?.InvoiceNo,
                        // CRITICAL: Map User (use CurrentUser helper for context-agnostic access)
                        // Note: Company property expects User type, but in Admin context we have ApplicationUser
                        // We'll set it to null and use CustomerName instead
                        Company = null, // Admin context'te User null, ApplicationUser var ama type uyumsuz
                        // CRITICAL: Set CustomerName using helper property (works in both contexts)
                        CustomerName = customerName,
                        Seller = order.Seller,
                        UserAddress = order.UserAddress,
                        Cargo = order.Cargo,
                        Bank = order.Bank,
                        OrderItems = order.OrderItems?.ToList() ?? new List<OrderItems>(),
                        
                        // New Creator Logic
                        IsCreatedByPlasiyer = isPlasiyer,
                        CreatorName = isPlasiyer 
                            ? (plasiyerNames.TryGetValue(order.CreatedId, out var pName) ? pName : "Plasiyer") 
                            : customerName // or "Müşteri" / "Kendisi"
                    };
                }).ToList();
                
                // Siparişlere bağlı tüm faturaları çek (LinkedInvoices)
                // İki yönlü ilişki: Invoice.OrderId → siparişe bağlı fatura, Orders.InvoiceId → faturaya bağlı sipariş
                var allOrderIds = mappedCats.Select(o => o.Id).ToList();
                var allInvoiceIds = mappedCats.Where(o => o.InvoiceId.HasValue).Select(o => o.InvoiceId!.Value).Distinct().ToList();
                if (allOrderIds.Any() || allInvoiceIds.Any())
                {
                    var linkedInvoices = await _context.DbContext.Set<ecommerce.Core.Entities.Accounting.Invoice>()
                        .AsNoTracking()
                        .Where(inv => inv.Status != (int)EntityStatus.Deleted &&
                            ((inv.OrderId.HasValue && allOrderIds.Contains(inv.OrderId.Value)) ||
                             allInvoiceIds.Contains(inv.Id)))
                        .ToListAsync();

                    foreach (var mappedOrder in mappedCats)
                    {
                        var faturaListesi = linkedInvoices
                            .Where(inv => inv.OrderId == mappedOrder.Id || inv.Id == mappedOrder.InvoiceId)
                            .GroupBy(inv => inv.Id) // Aynı fatura iki koşuldan da gelebilir, tekrarı önle
                            .Select(g => g.First())
                            .Select(inv => new OrderLinkedInvoiceDto
                            {
                                InvoiceId = inv.Id,
                                InvoiceNo = inv.InvoiceNo ?? "",
                                Ettn = inv.Ettn,
                                IsEInvoice = inv.IsEInvoice,
                                IsEArchive = inv.IsEArchive,
                                EInvoiceStatus = inv.EInvoiceStatus,
                                InvoiceDate = inv.InvoiceDate,
                                TotalAmount = inv.TotalAmount,
                                DiscountTotal = inv.DiscountTotal,
                                VatTotal = inv.VatTotal,
                                GeneralTotal = inv.GeneralTotal
                            }).ToList();

                        if (faturaListesi.Any())
                        {
                            mappedOrder.LinkedInvoices = faturaListesi;
                        }
                    }
                }

                var data = mappedCats.AsQueryable();
                data = data.OrderByDescending(x => x.Id);
                var result = _radzenPagerService.MakeDataQueryable(data, pager);
                response.Result = result;
                return response;
            } catch(Exception ex){
                _logger.LogError("GetOrders Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<DashboardOrderSummaryDto>> GetDashboardOrderSummary()
        {
            var rs = new IActionResult<DashboardOrderSummaryDto> { Result = new() };
            await _dbSemaphore.WaitAsync();
            try
            {
                // Capture context from outer scope
                var userPrincipal = _httpContextAccessor.HttpContext?.User;

                using (var scope = _scopeFactory.CreateScope())
                {
                    // Hydrate inner scope's CurrentUser to enable Global Filters via TenantProvider
                    if (userPrincipal != null)
                    {
                        var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
                        scopedCurrentUser.SetUser(userPrincipal);
                    }

                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();

                    // Global Query Filters (ApplicationDbContext) will automatically filter by BranchId via TenantProvider
                    var roleFilter = scope.ServiceProvider.GetRequiredService<ecommerce.Admin.Domain.Services.IRoleBasedFilterService>();

                    // Explicitly ignore leak-prone Global Filters and apply strict Role Filter
                    var orderQuery = uow.DbContext.Orders
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(x => x.Status != 99);
                    
                    orderQuery = ApplyOrderRoleFilter(orderQuery, uow.DbContext);

                    var stats = await orderQuery
                        .GroupBy(x => x.OrderStatusType)
                        .Select(g => new { 
                            Status = g.Key, 
                            Count = g.Count(), 
                            Revenue = g.Sum(x => x.GrandTotal) 
                        })
                        .ToListAsync();

                    rs.Result.TotalOrders = stats.Sum(x => x.Count);
                    rs.Result.TotalRevenue = stats.Sum(x => x.Revenue);

                    rs.Result.NewOrdersRevenue = stats.FirstOrDefault(x => x.Status == OrderStatusType.OrderNew)?.Revenue ?? 0;
                    rs.Result.CompletedOrdersRevenue = stats.FirstOrDefault(x => x.Status == OrderStatusType.OrderSuccess)?.Revenue ?? 0;
                    rs.Result.CancelledOrdersRevenue = stats.FirstOrDefault(x => x.Status == OrderStatusType.OrderCanceled)?.Revenue ?? 0;

                
                    rs.Result.PendingOrdersCount = stats
                        .Where(x => x.Status == OrderStatusType.OrderNew || x.Status == OrderStatusType.OrderWaitingApproval)
                        .Sum(x => x.Count);
                }
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetDashboardOrderSummary Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<IActionResult<List<DashboardBestSellingProductDto>>> GetBestSellingProducts(int topN)
        {
            var rs = new IActionResult<List<DashboardBestSellingProductDto>> { Result = new() };
            await _dbSemaphore.WaitAsync();
            try
            {
                // Capture context from outer scope
                var userPrincipal = _httpContextAccessor.HttpContext?.User;

                using (var scope = _scopeFactory.CreateScope())
                {
                    // Hydrate inner scope's CurrentUser to enable Global Filters via TenantProvider
                    if (userPrincipal != null)
                    {
                        var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
                        scopedCurrentUser.SetUser(userPrincipal);
                    }

                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                   
                    // Global Query Filters (ApplicationDbContext) will automatically filter by BranchId
                    // We need to join OrderItems with Orders to exclude deleted orders
                    // Use AsNoTracking for better performance and avoid null issues
                    // Note: Global filters on Order will apply
                    var roleFilter = scope.ServiceProvider.GetRequiredService<ecommerce.Admin.Domain.Services.IRoleBasedFilterService>();

                    // Prepare filtered Orders query
                    var ordersQuery = uow.DbContext.Orders.AsNoTracking().IgnoreQueryFilters();
                    ordersQuery = roleFilter.ApplyFilter(ordersQuery, uow.DbContext);

                    // Join with filtered orders
                    var query = from oi in uow.DbContext.OrderItems.AsNoTracking()
                                join o in ordersQuery on oi.OrderId equals o.Id
                                where o.Status != (int)EntityStatus.Deleted &&
                                      oi.ProductId > 0 &&
                                      !string.IsNullOrEmpty(oi.ProductName)
                                group oi by new { oi.ProductId, ProductName = oi.ProductName ?? string.Empty } into g
                                select new DashboardBestSellingProductDto
                                {
                                    ProductId = g.Key.ProductId,
                                    ProductName = g.Key.ProductName ?? string.Empty,
                                    ProductImage = string.Empty, // Will be populated later if needed
                                    TotalQuantitySold = g.Sum(x => x.Quantity),
                                    TotalRevenue = g.Sum(x => x.TotalPrice)
                                };

                    rs.Result = await query.OrderByDescending(x => x.TotalQuantitySold)
                                           .Take(topN)
                                           .ToListAsync();
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBestSellingProducts Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<IActionResult<List<DashboardSellerSalesDto>>> GetSalesBySeller(int topN)
        {
            var rs = new IActionResult<List<DashboardSellerSalesDto>> { Result = new() };
            await _dbSemaphore.WaitAsync();
            try
            {
                // Capture context from outer scope
                var userPrincipal = _httpContextAccessor.HttpContext?.User;

                using (var scope = _scopeFactory.CreateScope())
                {
                    // Hydrate inner scope's CurrentUser to enable Global Filters via TenantProvider
                    if (userPrincipal != null)
                    {
                        var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
                        scopedCurrentUser.SetUser(userPrincipal);
                    }

                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    
                    // Global Filters apply to Orders automatically
                    var roleFilter = scope.ServiceProvider.GetRequiredService<ecommerce.Admin.Domain.Services.IRoleBasedFilterService>();

                    // Prepare filtered Orders query
                    var ordersQuery = uow.DbContext.Orders.AsNoTracking().IgnoreQueryFilters();
                    ordersQuery = roleFilter.ApplyFilter(ordersQuery, uow.DbContext);
                    
                    // Join with filtered orders
                    var query = from s in uow.DbContext.Sellers.AsNoTracking()
                                join o in ordersQuery on s.Id equals o.SellerId
                                where o.Status != (int)EntityStatus.Deleted
                                group o by new { s.Id, s.Name } into g
                                select new DashboardSellerSalesDto
                                {
                                    SellerId = g.Key.Id,
                                    SellerName = g.Key.Name,
                                    OrderCount = g.Count(),
                                    TotalSalesAmount = g.Sum(x => x.GrandTotal)
                                };

                    rs.Result = await query.OrderByDescending(x => x.TotalSalesAmount)
                                           .Take(topN)
                                           .ToListAsync();
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSalesBySeller Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        public async Task<IActionResult<List<DashboardChartDto>>> GetOrderStatsOverTime(int days)
        {
            var rs = new IActionResult<List<DashboardChartDto>> { Result = new() };
            await _dbSemaphore.WaitAsync();
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var roleFilter = scope.ServiceProvider.GetRequiredService<ecommerce.Admin.Domain.Services.IRoleBasedFilterService>();
                    var startDate = DateTime.Today.AddDays(-days);

                    // Use strict role filter instead of manual logic
                    var ordersQuery = uow.DbContext.Orders.AsNoTracking().IgnoreQueryFilters()
                        .Where(o => o.Status != 99 && o.CreatedDate >= startDate);
                    
                    ordersQuery = roleFilter.ApplyFilter(ordersQuery, uow.DbContext);

                    var query = from o in ordersQuery
                                group o by new { o.CreatedDate.Year, o.CreatedDate.Month, o.CreatedDate.Day } into g
                                select new
                                {
                                    Date = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day),
                                    TotalRevenue = g.Sum(x => x.GrandTotal),
                                    OrderCount = g.Count()
                                };

                    var dbResult = await query.ToListAsync();

                    // Fill in missing days
                    var result = new List<DashboardChartDto>();
                    for (int i = 0; i <= days; i++)
                    {
                        var date = startDate.AddDays(i);
                        var dayData = dbResult.FirstOrDefault(x => x.Date == date);
                        result.Add(new DashboardChartDto
                        {
                            Date = date.ToString("dd.MM"),
                            TotalRevenue = dayData?.TotalRevenue ?? 0,
                            OrderCount = dayData?.OrderCount ?? 0
                        });
                    }

                    rs.Result = result;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetOrderStatsOverTime Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task<Dictionary<int, string>> GetOrderItemWarehouseStocks(int orderId)
        {
            var result = new Dictionary<int, string>();
            await _dbSemaphore.WaitAsync();
            try 
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                   
                    var uow = (IUnitOfWork<ApplicationDbContext>)scope.ServiceProvider.GetRequiredService(typeof(IUnitOfWork<ApplicationDbContext>));

                    var order = await uow.DbContext.Orders
                        .AsNoTracking()
                        .Include(x => x.OrderItems)
                        .FirstOrDefaultAsync(x => x.Id == orderId);

                    if (order == null || order.OrderItems == null) return result;

                    var productIds = order.OrderItems!.Select(x => x.ProductId).Distinct().ToList();

                    var groupCodes = await uow.DbContext.ProductGroupCodes
                        .AsNoTracking()
                        .Where(x => productIds.Contains(x.ProductId))
                        .ToListAsync();

                    var codes = groupCodes.Select(x => x.OemCode).Where(c => !string.IsNullOrEmpty(c)).ToList();

                    if (!codes.Any() && !order.OrderItems.Any(x => !string.IsNullOrEmpty(x.SourceId))) 
                    {
                        return result;
                    }

                    switch (order.SellerId)
                    {
                        case 1:
                            var otoQuery = uow.DbContext.ProductOtoIsmails.AsNoTracking();
                            var otoMatch = otoQuery.Where(x => false);
                            var otoSourceIds = order.OrderItems.Where(x => !string.IsNullOrEmpty(x.SourceId))
                                .Select(x => int.TryParse(x.SourceId, out var id) ? (int?)id : null)
                                .Where(id => id.HasValue).Select(id => id!.Value).ToList();
                            
                            if (otoSourceIds.Any())
                            {
                                otoMatch = otoMatch.Union(otoQuery.Where(x => otoSourceIds.Contains(x.Id)));
                            }
                            
                            foreach (var code in codes)
                            {
                                var c = code ?? "";
                                otoMatch = otoMatch.Union(otoQuery.Where(x => EF.Functions.ILike(x.Kod, $"%{c}%") || EF.Functions.ILike(x.GrupKodu, $"%{c}%")));
                            }
                            var otoInfoList = await otoMatch.ToListAsync();
                            
                            foreach (var oi in order.OrderItems)
                            {
                                var infos = new List<ProductOtoIsmail>();
                                if (!string.IsNullOrEmpty(oi.SourceId) && int.TryParse(oi.SourceId, out var sId))
                                {
                                    var match = otoInfoList.FirstOrDefault(x => x.Id == sId);
                                    if (match != null) infos.Add(match);
                                }
                                
                                if (!infos.Any())
                                {
                                    var gc = groupCodes.FirstOrDefault(x => x.ProductId == oi.ProductId);
                                    if (gc != null)
                                    {
                                        infos = otoInfoList.Where(x => 
                                            (x.Kod != null && x.Kod.Contains(gc.OemCode, StringComparison.OrdinalIgnoreCase)) || 
                                            (x.GrupKodu != null && x.GrupKodu.Contains(gc.OemCode, StringComparison.OrdinalIgnoreCase)))
                                            .ToList();
                                    }
                                }

                                if (infos.Any())
                                {
                                    var stocks = new List<string>();
                                    
                                    int gebze = infos.Max(x => x.Gebze ?? 0);
                                    int ankara = infos.Max(x => x.Ankara ?? 0);
                                    int ikitelli = infos.Max(x => x.Ikitelli ?? 0);
                                    int izmir = infos.Max(x => x.Izmir ?? 0);
                                    int samsun = infos.Max(x => x.Samsun ?? 0);
                                    int depo1030 = infos.Max(x => x.Depo1030 ?? 0);
                                    int depo13 = infos.Max(x => x.Depo13 ?? 0);

                                    if (gebze > 0) stocks.Add($"Gebze: {gebze}");
                                    if (ankara > 0) stocks.Add($"Ankara: {ankara}");
                                    if (ikitelli > 0) stocks.Add($"İkitelli: {ikitelli}");
                                    if (izmir > 0) stocks.Add($"İzmir: {izmir}");
                                    if (samsun > 0) stocks.Add($"Samsun: {samsun}");
                                    if (depo1030 > 0) stocks.Add($"Depo1030: {depo1030}");
                                    if (depo13 > 0) stocks.Add($"Depo13: {depo13}");

                                    result[oi.Id] = stocks.Any() ? string.Join(", ", stocks) : "Stok Yok";
                                }
                            }
                            break;
                        case 2:
                            var basbugQuery = uow.DbContext.ProductBasbugs.AsNoTracking();
                            var basbugMatch = basbugQuery.Where(x => false);
                            var basbugSourceIds = order.OrderItems.Where(x => !string.IsNullOrEmpty(x.SourceId))
                                .Select(x => int.TryParse(x.SourceId, out var id) ? (int?)id : null)
                                .Where(id => id.HasValue).Select(id => id!.Value).ToList();
                            
                            if (basbugSourceIds.Any())
                            {
                                basbugMatch = basbugMatch.Union(basbugQuery.Where(x => basbugSourceIds.Contains(x.Id)));
                            }

                            foreach (var code in codes)
                            {
                                var c = code ?? "";
                                basbugMatch = basbugMatch.Union(basbugQuery.Where(x => EF.Functions.ILike(x.No, $"%{c}%") || EF.Functions.ILike(x.GrupKod, $"%{c}%")));
                            }
                            var basbugInfoList = await basbugMatch.ToListAsync();
                            foreach (var oi in order.OrderItems!)
                            {
                                ProductBasbug? info = null;
                                if (!string.IsNullOrEmpty(oi.SourceId) && int.TryParse(oi.SourceId, out var sId))
                                {
                                    info = basbugInfoList.FirstOrDefault(x => x.Id == sId);
                                }
                                
                                if (info == null)
                                {
                                    var gc = groupCodes.FirstOrDefault(x => x.ProductId == oi.ProductId);
                                    if (gc != null)
                                    {
                                        info = basbugInfoList.FirstOrDefault(x => 
                                            (x.No != null && x.No.Contains(gc.OemCode, StringComparison.OrdinalIgnoreCase)) || 
                                            (x.GrupKod != null && x.GrupKod.Contains(gc.OemCode, StringComparison.OrdinalIgnoreCase)));
                                    }
                                }

                                if (info != null)
                                {
                                    result[oi.Id] = $"Stok: {info.Stok ?? 0}";
                                }
                            }
                            break;
                        case 3:
                            var degaQuery = uow.DbContext.ProductDegas.AsNoTracking();
                            var degaMatch = degaQuery.Where(x => false);
                            var degaSourceIds = order.OrderItems.Where(x => !string.IsNullOrEmpty(x.SourceId))
                                .Select(x => int.TryParse(x.SourceId, out var id) ? (int?)id : null)
                                .Where(id => id.HasValue).Select(id => id!.Value).ToList();
                            
                            if (degaSourceIds.Any())
                            {
                                degaMatch = degaMatch.Union(degaQuery.Where(x => degaSourceIds.Contains(x.Id)));
                            }

                            foreach (var code in codes)
                            {
                                var c = code ?? "";
                                degaMatch = degaMatch.Union(degaQuery.Where(x => EF.Functions.ILike(x.Code, $"%{c}%")));
                            }
                            var degaInfoList = await degaMatch.ToListAsync();
                            foreach (var oi in order.OrderItems!)
                            {
                                ProductDega? info = null;
                                if (!string.IsNullOrEmpty(oi.SourceId) && int.TryParse(oi.SourceId, out var sId))
                                {
                                    info = degaInfoList.FirstOrDefault(x => x.Id == sId);
                                }
                                
                                if (info == null)
                                {
                                    var gc = groupCodes.FirstOrDefault(x => x.ProductId == oi.ProductId);
                                    if (gc != null)
                                    {
                                        info = degaInfoList.FirstOrDefault(x => x.Code != null && x.Code.Contains(gc.OemCode, StringComparison.OrdinalIgnoreCase));
                                    }
                                }

                                if (info != null)
                                {
                                    var stocks = new List<string>();
                                    if (!string.IsNullOrEmpty(info.Depo1) && info.Depo1 != "0") stocks.Add($"Depo1: {info.Depo1}");
                                    if (!string.IsNullOrEmpty(info.Depo2) && info.Depo2 != "0") stocks.Add($"Depo2: {info.Depo2}");
                                    if (!string.IsNullOrEmpty(info.Depo3) && info.Depo3 != "0") stocks.Add($"Depo3: {info.Depo3}");
                                    if (!string.IsNullOrEmpty(info.Depo4) && info.Depo4 != "0") stocks.Add($"Depo4: {info.Depo4}");
                                    if (!string.IsNullOrEmpty(info.Depo5) && info.Depo5 != "0") stocks.Add($"Depo5: {info.Depo5}");
                                    if (!string.IsNullOrEmpty(info.Depo6) && info.Depo6 != "0") stocks.Add($"Depo6: {info.Depo6}");
                                    result[oi.Id] = stocks.Any() ? string.Join(", ", stocks) : "Stok Yok";
                                }
                            }
                            break;
                        case 4:
                            var remarQuery = uow.DbContext.ProductRemars.AsNoTracking();
                            var remarMatch = remarQuery.Where(x => false);
                            var remarSourceIds = order.OrderItems.Where(x => !string.IsNullOrEmpty(x.SourceId))
                                .Select(x => int.TryParse(x.SourceId, out var id) ? (int?)id : null)
                                .Where(id => id.HasValue).Select(id => id!.Value).ToList();
                            
                            if (remarSourceIds.Any())
                            {
                                remarMatch = remarMatch.Union(remarQuery.Where(x => remarSourceIds.Contains(x.Id)));
                            }

                            foreach (var code in codes)
                            {
                                var c = code ?? "";
                                remarMatch = remarMatch.Union(remarQuery.Where(x => EF.Functions.ILike(x.Code, $"%{c}%")));
                            }
                            var remarInfoList = await remarMatch.ToListAsync();
                            foreach (var oi in order.OrderItems!)
                            {
                                ProductRemar? info = null;
                                if (!string.IsNullOrEmpty(oi.SourceId) && int.TryParse(oi.SourceId, out var sId))
                                {
                                    info = remarInfoList.FirstOrDefault(x => x.Id == sId);
                                }
                                
                                if (info == null)
                                {
                                    var gc = groupCodes.FirstOrDefault(x => x.ProductId == oi.ProductId);
                                    if (gc != null)
                                    {
                                        info = remarInfoList.FirstOrDefault(x => x.Code != null && x.Code.Contains(gc.OemCode, StringComparison.OrdinalIgnoreCase));
                                    }
                                }

                                if (info != null)
                                {
                                    var stocks = new List<string>();
                                    if (!string.IsNullOrEmpty(info.Depo_1) && info.Depo_1 != "0") stocks.Add($"Depo_1: {info.Depo_1}");
                                    if (!string.IsNullOrEmpty(info.Depo_2) && info.Depo_2 != "0") stocks.Add($"Depo_2: {info.Depo_2}");
                                    result[oi.Id] = stocks.Any() ? string.Join(", ", stocks) : "Stok Yok";
                                }
                            }
                            break;
                    }
                }
                
                return result;
            }
            finally 
            {
                _dbSemaphore.Release();
            }
        }
        public async Task<Dictionary<int, string>> GetOrderItemApiStocks(int orderId)
        {
            var result = new Dictionary<int, string>();
            var order = await _context.DbContext.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductGroupCodes)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null || order.OrderItems == null) return result;

            var provider = _stockResolver.GetProvider(order.SellerId);
            if (provider == null)
            {
                _logger.LogWarning($"No real-time stock provider found for SellerId: {order.SellerId}");
                return result;
            }

            foreach (var item in order.OrderItems)
            {
                try
                {
                    // Senior approach: Use GroupCode if available, then Barcode, then SourceId (ProductCode)
                    string? lookupKey = item.Product?.ProductGroupCodes?.FirstOrDefault()?.OemCode 
                                       ?? item.Product?.Barcode 
                                       ?? item.SourceId;

                    if (string.IsNullOrEmpty(lookupKey)) 
                    {
                        _logger.LogWarning($"No lookup key found for item {item.Id} (ProductId: {item.ProductId})");
                        continue;
                    }


                    // OtoIsmail (SellerId == 1) ise SourceId (db id) üzerinden asıl Kod bilgisini alalım
                    if (order.SellerId == 1 && !string.IsNullOrEmpty(item.SourceId) && int.TryParse(item.SourceId, out int pOismlId))
                    {
                        var pOisml = await _context.DbContext.ProductOtoIsmails
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == pOismlId);
                        
                        if (pOisml != null)
                        {
                            // Kesin bilgi: Sadece tablodaki Kod alanına bakılsın
                            if (!string.IsNullOrEmpty(pOisml.Kod))
                            {
                                lookupKey = pOisml.Kod;
                            }

                            // 2024-01-10: Kullanıcı isteği üzerine API kontrolü şimdilik pasife alındı. 
                            // Web tarafında sepete eklerken kullanılacak. 
                            // Şimdilik sadece yerel DB'den en güncel (Max Aggregation) veriyi dönüyoruz.
                            /*
                            var stockInfo = await provider.GetStockAsync(lookupKey, item.SourceId);
                            
                            // Hibrit mantık: Eğer API'den sadece "VAR" (veya detay içermeyen "Stok Yok" ama db'den veri var) gelirse, 
                            // yerel veritabanındaki (manuel butonun baktığı) verileri döndürelim.
                            if (stockInfo == "VAR" || stockInfo == "Stok Yok")
                            */
                            {
                                // Potansiyel duplicate kayıtları yakalamak için hepsini çekip Max stokları alıyoruz
                                var matchingProducts = await _context.DbContext.ProductOtoIsmails
                                    .Where(p => p.NetsisStokId == pOisml.NetsisStokId)
                                    .ToListAsync();

                                if (matchingProducts.Any())
                                {
                                    var localStocks = new List<string>();

                                    int gebze = matchingProducts.Max(p => p.Gebze ?? 0);
                                    int ankara = matchingProducts.Max(p => p.Ankara ?? 0);
                                    int ikitelli = matchingProducts.Max(p => p.Ikitelli ?? 0);
                                    int izmir = matchingProducts.Max(p => p.Izmir ?? 0);
                                    int samsun = matchingProducts.Max(p => p.Samsun ?? 0);
                                    int depo1030 = matchingProducts.Max(p => p.Depo1030 ?? 0);
                                    int depo13 = matchingProducts.Max(p => p.Depo13 ?? 0);

                                    if (gebze > 0) localStocks.Add($"Gebze: {gebze}");
                                    if (ankara > 0) localStocks.Add($"Ankara: {ankara}");
                                    if (ikitelli > 0) localStocks.Add($"İkitelli: {ikitelli}");
                                    if (izmir > 0) localStocks.Add($"İzmir: {izmir}");
                                    if (samsun > 0) localStocks.Add($"Samsun: {samsun}");
                                    if (depo1030 > 0) localStocks.Add($"Depo1030: {depo1030}");
                                    if (depo13 > 0) localStocks.Add($"Depo13: {depo13}");
                                    
                                    if (localStocks.Any())
                                    {
                                        result[item.Id] = string.Join(", ", localStocks);
                                        continue;
                                    }
                                }
                            }
                            // result[item.Id] = stockInfo; // API sonucu pasif
                            continue; // Bu ürün için işlemi tamamladık
                        }
                    }

                    // Basbug (SellerId == 2) için lokal DB'den stok bilgisi al
                    // Basbug API grup bazlı çalıştığı için doğrudan kod sorgulaması yapılamıyor
                    if (order.SellerId == 2 && !string.IsNullOrEmpty(item.SourceId) && int.TryParse(item.SourceId, out int pBasbugId))
                    {
                        var pBasbug = await _context.DbContext.ProductBasbugs
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == pBasbugId);
                        
                        if (pBasbug != null)
                        {
                            // Basbug'da direkt lokal stok bilgisi kullanılıyor
                            if (pBasbug.Stok.HasValue && pBasbug.Stok.Value > 0)
                            {
                                result[item.Id] = $"Stok: {pBasbug.Stok.Value}";
                            }
                            else
                            {
                                result[item.Id] = "Stok Yok";
                            }
                            continue;
                        }
                    }

                    // Dega (SellerId == 3) ise asıl Code bilgisini alalım
                    if (order.SellerId == 3 && !string.IsNullOrEmpty(item.SourceId) && int.TryParse(item.SourceId, out int pDegaId))
                    {
                        var pDega = await _context.DbContext.ProductDegas
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == pDegaId);
                        
                        if (pDega != null)
                        {
                            if (!string.IsNullOrEmpty(pDega.Code))
                            {
                                lookupKey = pDega.Code;
                            }

                            var stockInfo = await provider.GetStockAsync(lookupKey, item.SourceId);
                            
                            if (stockInfo == "VAR" || (stockInfo == "Stok Yok" && !string.IsNullOrEmpty(pDega.Depo1)))
                            {
                                var localStocks = new List<string>();
                                if (!string.IsNullOrEmpty(pDega.Depo1) && pDega.Depo1 != "0") localStocks.Add($"Depo1: {pDega.Depo1}");
                                if (!string.IsNullOrEmpty(pDega.Depo2) && pDega.Depo2 != "0") localStocks.Add($"Depo2: {pDega.Depo2}");
                                if (!string.IsNullOrEmpty(pDega.Depo3) && pDega.Depo3 != "0") localStocks.Add($"Depo3: {pDega.Depo3}");
                                if (!string.IsNullOrEmpty(pDega.Depo4) && pDega.Depo4 != "0") localStocks.Add($"Depo4: {pDega.Depo4}");
                                if (!string.IsNullOrEmpty(pDega.Depo5) && pDega.Depo5 != "0") localStocks.Add($"Depo5: {pDega.Depo5}");
                                if (!string.IsNullOrEmpty(pDega.Depo6) && pDega.Depo6 != "0") localStocks.Add($"Depo6: {pDega.Depo6}");
                                
                                if (localStocks.Any())
                                {
                                    stockInfo = string.Join(", ", localStocks);
                                }
                            }
                            result[item.Id] = stockInfo;
                            continue;
                        }
                    }

                    // Remar (SellerId == 4) ise asıl Code bilgisini alalım
                    if (order.SellerId == 4 && !string.IsNullOrEmpty(item.SourceId) && int.TryParse(item.SourceId, out int pRemarId))
                    {
                        var pRemar = await _context.DbContext.ProductRemars
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == pRemarId);
                        
                        if (pRemar != null)
                        {
                            if (!string.IsNullOrEmpty(pRemar.Code))
                            {
                                lookupKey = pRemar.Code;
                            }

                            var stockInfo = await provider.GetStockAsync(lookupKey, item.SourceId);
                            
                            if (stockInfo == "VAR" || (stockInfo == "Stok Yok" && !string.IsNullOrEmpty(pRemar.Depo_1)))
                            {
                                var localStocks = new List<string>();
                                if (!string.IsNullOrEmpty(pRemar.Depo_1) && pRemar.Depo_1 != "0") localStocks.Add($"Depo_1: {pRemar.Depo_1}");
                                if (!string.IsNullOrEmpty(pRemar.Depo_2) && pRemar.Depo_2 != "0") localStocks.Add($"Depo_2: {pRemar.Depo_2}");
                                
                                if (localStocks.Any())
                                {
                                    stockInfo = string.Join(", ", localStocks);
                                }
                            }
                            result[item.Id] = stockInfo;
                            continue;
                        }
                    }

                    var genericStockInfo = await provider.GetStockAsync(lookupKey, item.SourceId);
                    result[item.Id] = genericStockInfo;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching real-time stock for item {item.Id}");
                    result[item.Id] = "Hata";
                }
            }

            return result;
        }

        public async Task<string?> GetProductCodeForCart(int sellerId, int sourceId)
        {
            try
            {
                return sellerId switch
                {
                    1 => (await _context.DbContext.ProductOtoIsmails.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId))?.Kod,
                    2 => (await _context.DbContext.ProductBasbugs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId))?.No,
                    3 => (await _context.DbContext.ProductDegas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId))?.Code,
                    4 => (await _context.DbContext.ProductRemars.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceId))?.Code,
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product code for SellerId: {SellerId}, SourceId: {SourceId}", sellerId, sourceId);
                return null;
            }
        }
        public async Task UpdateOrderItem(OrderItems item)
        {
            try
            {
                _context.DbContext.OrderItems.Update(item);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OrderItem {ItemId}", item.Id);
                throw;
            }
        }
        
        // B2B: Get orders for current logged-in user (ApplicationUser)
        public async Task<IActionResult<List<OrderListDto>>> GetMyOrders(int? userId = null)
        {
            var response = OperationResult.CreateResult<List<OrderListDto>>();
            try
            {
                // If userId not provided, get from HttpContext
                if (!userId.HasValue)
                {
                    var principal = _httpContextAccessor.HttpContext?.User;
                    var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                    {
                        response.AddError("Unauthorized");
                        return response;
                    }
                    userId = currentUserId;
                }
                
                // Use injected context directly to preserve Request Scope and CurrentUser state
                // using (var scope = _scopeFactory.CreateScope())
                {
                    // var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    // var repo = uow.GetRepository<Orders>();
                    var repo = _context.GetRepository<Orders>();
                    
                    // Get orders for this user (ApplicationUser via CompanyId) with AsNoTracking to avoid tracking conflicts
                    var ordersQuery = repo.GetAll(
                        predicate: f => f.CompanyId == userId.Value && f.Status == (int)EntityStatus.Active,
                        include: x => x.Include(f => f.OrderItems)
                                       .ThenInclude(p => p.Product)
                                       .ThenInclude(p => p.ProductImage)
                                       .Include(x => x.Seller)
                                       .Include(x => x.ApplicationUser)
                                       .Include(x => x.UserAddress)
                                       .ThenInclude(ua => ua!.City)
                                       .Include(x => x.UserAddress)
                                       .ThenInclude(ua => ua!.Town)
                                       .Include(x => x.Invoice)
                                       .Include(x => x.Cargo!)
                                       .Include(x => x.Bank!)
                    ).AsNoTracking();
                    
                    // PERFORMANCE FIX: Take last 250 orders
                    var ordersList = await ordersQuery.OrderByDescending(o => o.CreatedDate).Take(250).ToListAsync();
                    
                    // Map to DTO
                    var mappedOrders = ordersList.Select(order => new OrderListDto
                    {
                        Id = order.Id,
                        OrderNumber = order.OrderNumber,
                        OrderStatusType = order.OrderStatusType,
                        PaymentTypeId = order.PaymentTypeId,
                        CreatedDate = order.CreatedDate,
                        ShipmentDate = order.OrderItems?.FirstOrDefault()?.ShipmentDate ?? order!.CreatedDate, // Use OrderItems
                        DeliveryDate = order.DeliveryDate,
                        CargoPrice = order.CargoPrice,
                        CargoTrackNumber = order.OrderItems?.FirstOrDefault()?.CargoTrackNumber, // Use OrderItems
                        CargoTrackUrl = order.OrderItems?.FirstOrDefault()?.CargoTrackUrl, // Use OrderItems
                        DiscountTotal = order.DiscountTotal,
                        ProductTotal = order.ProductTotal,
                        OrderTotal = order.OrderTotal,
                        GrandTotal = order.GrandTotal,
                        PaymentStatus = order.PaymentStatus,
                        IyzicoCancelDate = order.IyzicoCancelDate,
                        IyzicoCanceledMessage = order.IyzicoCanceledMessage,
                        IyzicoPaidTotal = order.IyzicoPaidTotal,
                        CompanyId = order.CompanyId,
                        Company = null, // Admin context'te User null
                        CustomerName = !string.IsNullOrWhiteSpace(order.UserAddress?.FullName) 
                            ? (order.UserAddress?.FullName ?? "") 
                            : (order.UserFullName ?? ""),
                        Seller = order.Seller,
                        UserAddress = order.UserAddress,
                        Cargo = order.Cargo,
                        Bank = order.Bank,
                        OrderItems = order.OrderItems?.ToList() ?? new List<OrderItems>(),
                        InvoiceId = order.InvoiceId,
                        InvoiceNo = order.Invoice?.InvoiceNo
                    }).ToList();
                    
                    response.Result = mappedOrders;
                    
                    // Siparişlere bağlı tüm faturaları çek (LinkedInvoices)
                    // İki yönlü ilişki: Invoice.OrderId → siparişe bağlı fatura, Orders.InvoiceId → faturaya bağlı sipariş
                    var orderIds = mappedOrders.Select(o => o.Id).ToList();
                    var invoiceIds = mappedOrders.Where(o => o.InvoiceId.HasValue).Select(o => o.InvoiceId!.Value).Distinct().ToList();
                    if (orderIds.Any() || invoiceIds.Any())
                    {
                        var linkedInvoices = await _context.DbContext.Set<ecommerce.Core.Entities.Accounting.Invoice>()
                            .AsNoTracking()
                            .Where(inv => inv.Status != (int)EntityStatus.Deleted &&
                                ((inv.OrderId.HasValue && orderIds.Contains(inv.OrderId.Value)) ||
                                 invoiceIds.Contains(inv.Id)))
                            .Select(inv => new { inv.OrderId, inv.Id, inv.InvoiceNo, inv.Ettn, inv.IsEInvoice, inv.IsEArchive, inv.EInvoiceStatus })
                            .ToListAsync();
                        
                        foreach (var order in mappedOrders)
                        {
                            var faturaListesi = linkedInvoices
                                .Where(inv => inv.OrderId == order.Id || inv.Id == order.InvoiceId)
                                .GroupBy(inv => inv.Id)
                                .Select(g => g.First())
                                .Select(inv => new OrderLinkedInvoiceDto
                                {
                                    InvoiceId = inv.Id,
                                    InvoiceNo = inv.InvoiceNo,
                                    Ettn = inv.Ettn,
                                    IsEInvoice = inv.IsEInvoice,
                                    IsEArchive = inv.IsEArchive,
                                    EInvoiceStatus = inv.EInvoiceStatus
                                })
                                .ToList();
                            
                            if (faturaListesi.Any())
                            {
                                order.LinkedInvoices = faturaListesi;
                            }
                        }
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetMyOrders Exception: " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<(bool Success, string Message)> CancelMyOrder(int orderId, int? userId = null)
        {
            try
            {
                // If userId not provided, get from HttpContext
                if (!userId.HasValue)
                {
                    var principal = _httpContextAccessor.HttpContext?.User;
                    var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                    {
                        return (false, "Yetkilendirme hatası");
                    }
                    userId = currentUserId;
                }

                // Get order with all necessary includes
                var order = await _repository.GetFirstOrDefaultAsync(
                    predicate: o => o.Id == orderId && o.CompanyId == userId.Value && o.Status == (int)EntityStatus.Active,
                    include: q => q.Include(o => o.Bank!)
                                   .Include(o => o.ApplicationUser)
                                   .Include(o => o.Seller)
                                   .Include(o => o.OrderItems)
                                   .Include(o => o.Cargo!),
                    disableTracking: false
                );

                if (order == null)
                {
                    return (false, "Sipariş bulunamadı");
                }

                // Only allow cancellation for new/pending orders
                if (order.OrderStatusType != OrderStatusType.OrderNew && order.OrderStatusType != OrderStatusType.OrderWaitingPayment)
                {
                    return (false, "Bu sipariş artık iptal edilemez");
                }

                // STEP 1: Cancel the order status
                order.OrderStatusType = OrderStatusType.OrderCanceled;
                _context.DbContext.Entry(order).Property(x => x.OrderStatusType).IsModified = true;
                await _context.SaveChangesAsync();
                                // Verify the change was saved
                if (!_context.LastSaveChangesResult.IsOk)
                {
                    _logger.LogError($"CancelMyOrder: Failed to save OrderStatusType change. Error: {_context.LastSaveChangesResult.Exception?.Message}");
                    return (false, "Sipariş durumu güncellenemedi. Lütfen tekrar deneyiniz.");
                }

                // STEP 2: Attempt payment refund if payment was successful
                string refundMessage = "";
                if (order.PaymentStatus && order.Bank != null && !string.IsNullOrEmpty(order.PaymentId))
                {
                    try
                    {
                        var cancelPaymentStatus = await _paymentService.PaymentCancel(new CreateCancelRequest
                        {
                            PaymentId = order.PaymentId,
                            Ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                            Locale = Locale.TR.ToString(),
                            ConversationId = Guid.NewGuid().ToString(),
                        });

                        if (cancelPaymentStatus.Status != "success")
                        {
                            refundMessage = $" (Ödeme iptali başarısız: {cancelPaymentStatus.ErrorMessage})";
                        }
                        else
                        {
                            refundMessage = $" {order.GrandTotal:F2} TL iade işlemi başlatıldı.";
                            order.IyzicoCancelDate = DateTime.Now;
                            order.IyzicoCanceledMessage = $"B2B kullanıcı tarafından iptal edildi. Sipariş No: {order.OrderNumber}";
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"CancelMyOrder refund error: {ex.Message}");
                        refundMessage = $" (İade sırasında hata: {ex.Message})";
                    }
                }

                // STEP 3: Cancel cargo if exists
                if (order.Cargo != null)
                {
                    try
                    {
                        if (order.Cargo.Name.ToLower().Contains("mng"))
                        {
                            await _hangfireJobManager.EnqueueAsync<MngOrderCancelJob>(new MngOrderCancelJobArgs { OrderId = order.Id });
                        }
                        else if (order.Cargo.Name.ToLower().Contains("sendeo"))
                        {
                            await _hangfireJobManager.EnqueueAsync<SendeoOrderCancelJob>(new SendeoOrderCancelJobArgs { OrderId = order.Id });
                        }
                        else if (order.Cargo.Name.ToLower(new CultureInfo("tr-TR")).Contains("yurtiçi"))
                        {
                            await _hangfireJobManager.EnqueueAsync<YurticiOrderCancelJob>(new YurticiOrderCancelJobArgs { OrderId = order.Id });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"CancelMyOrder cargo cancel error: {ex.Message}");
                    }
                }

                // STEP 4: Send cancellation emails
                try
                {
                    await _emailService.SendOrderCancelledCustomerEmail(order);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"CancelMyOrder email error: {ex.Message}");
                }

                return (true, $"Sipariş iptal edildi.{refundMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"CancelMyOrder Exception: {ex.ToString()}");
                return (false, $"Sipariş iptal edilirken hata oluştu: {ex.Message}");
            }
        }

        public async Task<IActionResult<List<OrderListDto>>> GetUnfacturedOrders()
        {
            var response = new IActionResult<List<OrderListDto>> { Result = new List<OrderListDto>() };
            try
            {
                _logger.LogInformation("GetUnfacturedOrders - Fetching orders without InvoiceId");
                
                using (var scope = _scopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Orders>();
                    
                    // Global query filter bypass — doğrudan BranchId filtreleme yapılacak
                    var ordersQuery = repo.GetAll(
                        predicate: f => f.InvoiceId == null && f.OrderStatusType != OrderStatusType.OrderCanceled,
                        include: x => x.Include(f => f.OrderItems)
                                       .Include(x => x.ApplicationUser)
                                           .ThenInclude(u => u!.Customer)
                                       .Include(x => x.UserAddress!),
                        ignoreQueryFilters: true
                    ).AsNoTracking();
                    
                    // Role-based filtering — GetOrders ile aynı pattern
                    ordersQuery = ApplyOrderRoleFilter(ordersQuery, uow.DbContext);
                    
                    var ordersList = await ordersQuery.OrderByDescending(o => o.CreatedDate).ToListAsync();
                    
                    _logger.LogInformation("GetUnfacturedOrders - Found {Count} unfactured orders after filter", ordersList.Count);
                    
                    var mappedOrders = ordersList.Select(order => new OrderListDto
                    {
                        Id = order.Id,
                        OrderNumber = order.OrderNumber,
                        OrderStatusType = order.OrderStatusType,
                        PlatformType = order.PlatformType,
                        PaymentTypeId = order.PaymentTypeId,
                        CreatedDate = order.CreatedDate,
                        ShipmentDate = order.OrderItems?.FirstOrDefault()?.ShipmentDate ?? order.CreatedDate,
                        CargoPrice = order.CargoPrice,
                        DiscountTotal = order.DiscountTotal,
                        ProductTotal = order.ProductTotal,
                        OrderTotal = order.OrderTotal,
                        GrandTotal = order.GrandTotal,
                        PaymentStatus = order.PaymentStatus,
                        InvoiceId = order.InvoiceId,
                        // Mapping Logic:
                        // CustomerName = B2B Customer Name (Cari) -> ApplicationUser.Customer.Name
                        // If no B2B Customer, fall back to ApplicationUser Name
                        CustomerName = order.ApplicationUser?.Customer?.Name 
                            ?? (order.ApplicationUser != null ? $"{order.ApplicationUser!.FirstName} {order.ApplicationUser.LastName}" : "Bilinmiyor"),
                        
                        // BuyerName = Delivery Contact Name (Alıcı) -> UserAddress.FullName
                        BuyerName = !string.IsNullOrWhiteSpace(order.UserAddress?.FullName) 
                            ? (order.UserAddress?.FullName ?? "") 
                            : (order.ApplicationUser != null ? $"{order.ApplicationUser.FirstName} {order.ApplicationUser.LastName}" : ""),
                            
                        CustomerId = order.ApplicationUser?.CustomerId, // Set real B2B Customer ID for grouping validation
                        
                        // Avoid circular references by not mapping full navigation properties
                        Seller = null!,
                        UserAddress = null,
                        Cargo = null,
                        Bank = null,
                        Company = null, 
                        // Ürün detaylarını da map'le — accordion'da gösterilecek
                        OrderItems = order.OrderItems?.Select(oi => new OrderItems 
                        { 
                            Id = oi.Id,
                            ProductName = oi.ProductName,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            TotalPrice = oi.TotalPrice,
                            DiscountAmount = oi.DiscountAmount
                        }).ToList() ?? new List<OrderItems>()
                    }).ToList();
                    
                    response.Result = mappedOrders;
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUnfacturedOrders Exception");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<List<OrderListDto>>> GetOrdersByIds(List<int> ids)
        {
            IActionResult<List<OrderListDto>> response = new() { Result = new List<OrderListDto>() };
            try
            {
                var distinctIds = ids.Distinct().ToList();
                
                // Yeni scope kullan — UpsertInvoice.LoadData() aynı DbContext'i kullandığı için
                // AsSplitQuery ile çakışma olmaması adına bağımsız context gerekli
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                
                var data = await uow.DbContext.Orders
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(x => x.OrderItems).ThenInclude(p => p.Product).ThenInclude(p => p.ProductImage)
                    .Include(x => x.OrderItems).ThenInclude(p => p.Product).ThenInclude(p => p.Tax)
                    .Include(x => x.OrderItems).ThenInclude(p => p.Product).ThenInclude(p => p.ProductUnits)
                    .Include(x => x.OrderItems).ThenInclude(oi => oi.AppliedDiscounts).ThenInclude(ad => ad.Discount)
                    .Include(x => x.Seller).ThenInclude(s => s.City)
                    .Include(x => x.Seller).ThenInclude(s => s.Town)
                    .Include(x => x.ApplicationUser)
                    .Include(x => x.UserAddress).ThenInclude(ua => ua.City)
                    .Include(x => x.UserAddress).ThenInclude(ua => ua.Town)
                    .Include(x => x.Cargo)
                    .Include(x => x.Bank)
                    .Include(x => x.Invoice)
                    .Where(x => distinctIds.Contains(x.Id))
                    .ToListAsync();

                var mapped = mapper.Map<List<OrderListDto>>(data);
                if (mapped != null)
                {
                   
                    for (int i = 0; i < mapped.Count; i++)
                    {
                        var m = mapped[i];
                        var d = data.FirstOrDefault(x => x.Id == m.Id);
                        if (d != null)
                        {
                            if (string.IsNullOrWhiteSpace(m.CustomerName))
                            {
                                m.CustomerName = d.ApplicationUser != null 
                                    ? (d.ApplicationUser!.FullName ?? (d.ApplicationUser.FirstName + " " + d.ApplicationUser.LastName)) 
                                    : "Bilinmiyor";
                            }
                            m.CustomerId = d.ApplicationUser?.CustomerId;
                            
                            // OrderItems'ın map'lendiğinden emin ol
                            if ((m.OrderItems == null || !m.OrderItems.Any()) && d.OrderItems != null && d.OrderItems.Any())
                            {
                                m.OrderItems = d.OrderItems!.ToList();
                            }
                        }
                    }
                }
                response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetOrdersByIds Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<List<OrderListDto>>> GetCustomerOrders(int customerId)
        {
            IActionResult<List<OrderListDto>> response = new() { Result = new List<OrderListDto>() };
            try
            {
                // Use injected context directly
                // using var scope = _scopeFactory.CreateScope();
                // var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var context = _context.DbContext;
                
                var allCustomerUserIds = await context.Set<ApplicationUser>()
                    .AsNoTracking()
                    .Where(u => u.CustomerId == customerId)
                    .Select(u => u.Id)
                    .ToListAsync();
                
                _logger.LogInformation("GetCustomerOrders - CustomerId: {CustomerId}, LinkedUserIds: {UserIds}", 
                    customerId, string.Join(", ", allCustomerUserIds));
                
                // Query orders where:
                // 1. ApplicationUser.CustomerId matches (direct orders by customer users)
                // OR
                // 2. UserAddress.ApplicationUserId is in allCustomerUserIds (orders on behalf of customer by Plasiyer)
                var data = await context.Orders
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Include(x => x.OrderItems)
                    .Include(x => x.Seller)
                    .Include(x => x.ApplicationUser)
                        .ThenInclude(au => au!.Customer)
                    .Include(x => x.User)
                    .Include(x => x.UserAddress)
                        .ThenInclude(ua => ua!.ApplicationUser)
                    .Include(x => x.UserAddress)
                        .ThenInclude(ua => ua!.City)
                    .Include(x => x.UserAddress)
                        .ThenInclude(ua => ua!.Town)
                    .Where(x => x.Status != (int)EntityStatus.Deleted && 
                               ((x.ApplicationUser != null && x.ApplicationUser.CustomerId == customerId) ||
                                (allCustomerUserIds.Contains(x.CompanyId)) ||
                                (x.UserAddress != null && x.UserAddress.ApplicationUserId.HasValue && allCustomerUserIds.Contains(x.UserAddress.ApplicationUserId!.Value)) ||
                                context.CustomerAccountTransactions.Any(t => t.OrderId == x.Id && t.CustomerId == customerId)))
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                _logger.LogInformation("GetCustomerOrders - Found {Count} orders for CustomerId: {CustomerId}", 
                    data.Count, customerId);

                // Fetch creator names for plasiyer mapping
                var createdIds = data.Select(x => x.CreatedId).Distinct().ToList();
                var plasiyerNames = await context.Set<ApplicationUser>()
                    .AsNoTracking()
                    .Where(u => createdIds.Contains(u.Id) && u.SalesPersonId.HasValue)
                    .ToDictionaryAsync(u => u.Id, u => u.FullName);

                var customerName = data.FirstOrDefault(x => x.ApplicationUser?.Customer != null)?.ApplicationUser?.Customer?.Name 
                    ?? data.FirstOrDefault(x => x.UserAddress?.ApplicationUser?.Customer != null)?.UserAddress?.ApplicationUser?.Customer?.Name 
                    ?? "Müşteri";

                var mappedData = _mapper.Map<List<OrderListDto>>(data);

                foreach (var dto in mappedData)
                {
                    var originalOrder = data.FirstOrDefault(x => x.Id == dto.Id);
                    if (originalOrder != null)
                    {
                        var isPlasiyer = plasiyerNames.TryGetValue(originalOrder.CreatedId, out var pName);
                        dto.IsCreatedByPlasiyer = isPlasiyer;
                        dto.CreatorName = isPlasiyer ? pName : customerName;
                    }
                }

                response.Result = mappedData;
                
                // Siparişlere bağlı tüm faturaları çek (LinkedInvoices)
                // İki yönlü ilişki: Invoice.OrderId → siparişe bağlı fatura, Orders.InvoiceId → faturaya bağlı sipariş
                var orderIds = mappedData.Select(o => o.Id).ToList();
                var invoiceIds = mappedData.Where(o => o.InvoiceId.HasValue).Select(o => o.InvoiceId!.Value).Distinct().ToList();
                if (orderIds.Any() || invoiceIds.Any())
                {
                    var linkedInvoices = await context.Set<ecommerce.Core.Entities.Accounting.Invoice>()
                        .AsNoTracking()
                        .Where(inv => inv.Status != (int)EntityStatus.Deleted &&
                            ((inv.OrderId.HasValue && orderIds.Contains(inv.OrderId.Value)) ||
                             invoiceIds.Contains(inv.Id)))
                        .Select(inv => new { inv.OrderId, inv.Id, inv.InvoiceNo, inv.Ettn, inv.IsEInvoice, inv.IsEArchive, inv.EInvoiceStatus })
                        .ToListAsync();
                    
                    foreach (var order in mappedData)
                    {
                        var faturaListesi = linkedInvoices
                            .Where(inv => inv.OrderId == order.Id || inv.Id == order.InvoiceId)
                            .GroupBy(inv => inv.Id)
                            .Select(g => g.First())
                            .Select(inv => new OrderLinkedInvoiceDto
                            {
                                InvoiceId = inv.Id,
                                InvoiceNo = inv.InvoiceNo,
                                Ettn = inv.Ettn,
                                IsEInvoice = inv.IsEInvoice,
                                IsEArchive = inv.IsEArchive,
                                EInvoiceStatus = inv.EInvoiceStatus
                            })
                            .ToList();
                        
                        if (faturaListesi.Any())
                        {
                            order.LinkedInvoices = faturaListesi;
                        }
                    }
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerOrders Exception customerId: {CustomerId}", customerId);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<List<ProductPurchaseHistoryItemDto>> GetProductPurchaseHistoryByCustomer(int customerId, int productId)
        {
            var context = _context.DbContext;
            var allCustomerUserIds = await context.Set<ApplicationUser>()
                .AsNoTracking()
                .Where(u => u.CustomerId == customerId)
                .Select(u => u.Id)
                .ToListAsync();

            var orderIds = await context.Orders
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(x => x.Status != (int)EntityStatus.Deleted &&
                    x.OrderStatusType != OrderStatusType.OrderCanceled &&
                    ((x.ApplicationUser != null && x.ApplicationUser.CustomerId == customerId) ||
                     allCustomerUserIds.Contains(x.CompanyId) ||
                     (x.UserAddress != null && x.UserAddress.ApplicationUserId.HasValue && allCustomerUserIds.Contains(x.UserAddress.ApplicationUserId.Value)) ||
                     context.CustomerAccountTransactions.Any(t => t.OrderId == x.Id && t.CustomerId == customerId)))
                .Select(o => o.Id)
                .ToListAsync();

            if (orderIds.Count == 0) return new List<ProductPurchaseHistoryItemDto>();

            var items = await context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.ProductId == productId && orderIds.Contains(oi.OrderId))
                .Join(context.Orders.AsNoTracking().IgnoreQueryFilters(),
                    oi => oi.OrderId,
                    o => o.Id,
                    (oi, o) => new { oi, o })
                .Join(context.Set<Seller>().AsNoTracking(),
                    x => x.o.SellerId,
                    s => s.Id,
                    (x, s) => new ProductPurchaseHistoryItemDto
                    {
                        OrderId = x.o.Id,
                        OrderNumber = x.o.OrderNumber,
                        OrderCreatedDate = x.o.CreatedDate,
                        Quantity = x.oi.Quantity,
                        UnitPrice = x.oi.Price,
                        TotalPrice = x.oi.TotalPrice,
                        OrderStatusType = x.o.OrderStatusType,
                        SellerName = s.Name ?? ""
                    })
                .OrderByDescending(x => x.OrderCreatedDate)
                .ToListAsync();
            return items;
        }

        public async Task<List<int>> GetPurchasedProductIdsByCustomer(int customerId, IEnumerable<int> productIds)
        {
            var ids = productIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0) return new List<int>();

            var context = _context.DbContext;
            var allCustomerUserIds = await context.Set<ApplicationUser>()
                .AsNoTracking()
                .Where(u => u.CustomerId == customerId)
                .Select(u => u.Id)
                .ToListAsync();

            var orderIds = await context.Orders
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(x => x.Status != (int)EntityStatus.Deleted &&
                    x.OrderStatusType != OrderStatusType.OrderCanceled &&
                    ((x.ApplicationUser != null && x.ApplicationUser.CustomerId == customerId) ||
                     allCustomerUserIds.Contains(x.CompanyId) ||
                     (x.UserAddress != null && x.UserAddress.ApplicationUserId.HasValue && allCustomerUserIds.Contains(x.UserAddress.ApplicationUserId.Value)) ||
                     context.CustomerAccountTransactions.Any(t => t.OrderId == x.Id && t.CustomerId == customerId)))
                .Select(o => o.Id)
                .ToListAsync();

            if (orderIds.Count == 0) return new List<int>();

            var purchased = await context.OrderItems
                .AsNoTracking()
                .Where(oi => ids.Contains(oi.ProductId) && orderIds.Contains(oi.OrderId))
                .Select(oi => oi.ProductId)
                .Distinct()
                .ToListAsync();
            return purchased;
        }

        // B2B: Get orders for all customers linked to this Plasiyer
        public async Task<IActionResult<List<OrderListDto>>> GetPlasiyerCustomersOrders(int userId)
        {
            var response = OperationResult.CreateResult<List<OrderListDto>>();
            try
            {
                // Use injected context directly
                // using var scope = _scopeFactory.CreateScope();
                // var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var uow = _context;
                
                // 1. Get Plasiyer (SalesPersonId)
                var userRepo = uow.GetRepository<ApplicationUser>();
                var appUser = await userRepo.GetFirstOrDefaultAsync(predicate: u => u.Id == userId);
                
                if (appUser == null || !appUser.SalesPersonId.HasValue)
                {
                    // Not a linked plasiyer or user not found
                    response.Result = new List<OrderListDto>();
                    return response;
                }
                
                // 2. Get Linked Customer IDs
                var customerIds = await uow.DbContext.CustomerPlasiyers
                    .AsNoTracking()
                    .Where(cp => cp.SalesPersonId == appUser.SalesPersonId.Value)
                    .Select(cp => cp.CustomerId)
                    .ToListAsync();
                    
                if (!customerIds.Any())
                {
                    response.Result = new List<OrderListDto>();
                    return response;
                }
                
                _logger.LogInformation("GetPlasiyerCustomersOrders - UserId: {UserId}, LinkedCustomerIds: {CustomerIds}", 
                    userId, string.Join(", ", customerIds));
                
                var allCustomerUserIds = await uow.DbContext.Set<ApplicationUser>()
                    .AsNoTracking()
                    .Where(u => u.CustomerId.HasValue && customerIds.Contains(u.CustomerId.Value))
                    .Select(u => u.Id)
                    .ToListAsync();
                
                _logger.LogInformation("GetPlasiyerCustomersOrders - Found {Count} user IDs for customer IDs", 
                    allCustomerUserIds.Count);
                
                // 4. Get Orders for these Customers - using same logic as GetCustomerOrders
                var ordersList = await uow.DbContext.Orders
                    .AsNoTracking()
                    .Include(x => x.OrderItems)
                    .Include(x => x.ApplicationUser)
                        .ThenInclude(au => au!.Customer)
                    .Include(x => x.User)
                    .Include(x => x.UserAddress)
                        .ThenInclude(ua => ua!.ApplicationUser)
                            .ThenInclude(u => u!.Customer)
                    .Include(x => x.UserAddress)
                        .ThenInclude(ua => ua!.City)
                    .Include(x => x.UserAddress)
                        .ThenInclude(ua => ua!.Town)
                    .Include(x => x.Invoice)
                    .Include(x => x.Seller)
                    .Where(x => x.Status != (int)EntityStatus.Deleted &&
                               ((x.ApplicationUser != null && x.ApplicationUser.CustomerId.HasValue && customerIds.Contains(x.ApplicationUser.CustomerId!.Value)) ||
                                (allCustomerUserIds.Contains(x.CompanyId)) ||
                                (x.UserAddress != null && x.UserAddress.ApplicationUserId.HasValue && allCustomerUserIds.Contains(x.UserAddress.ApplicationUserId!.Value)) ||
                                uow.DbContext.CustomerAccountTransactions.Any(t => t.OrderId == x.Id && customerIds.Contains(t.CustomerId))))
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(500) 
                    .ToListAsync();
                
                _logger.LogInformation("GetPlasiyerCustomersOrders - Found {Count} orders for UserId: {UserId}", 
                    ordersList.Count, userId);

                // Fetch creator names for plasiyer mapping
                var createdIds = ordersList.Select(x => x.CreatedId).Distinct().ToList();
                var plasiyerNames = await uow.DbContext.Set<ApplicationUser>()
                    .AsNoTracking()
                    .Where(u => createdIds.Contains(u.Id) && u.SalesPersonId.HasValue)
                    .ToDictionaryAsync(u => u.Id, u => u.FullName);

                var mappedData = _mapper.Map<List<OrderListDto>>(ordersList);

                foreach (var dto in mappedData)
                {
                    var originalOrder = ordersList.FirstOrDefault(x => x.Id == dto.Id);
                    if (originalOrder != null)
                    {
                        var isPlasiyer = plasiyerNames.TryGetValue(originalOrder.CreatedId, out var pName);
                        dto.IsCreatedByPlasiyer = isPlasiyer;
                        dto.CreatorName = isPlasiyer ? pName : (originalOrder.ApplicationUser?.Customer?.Name ?? "Müşteri");
                        
                        // CRITICAL: Manually set CustomerName from ApplicationUser.Customer
                        if (originalOrder.ApplicationUser?.Customer != null && !string.IsNullOrWhiteSpace(originalOrder.ApplicationUser.Customer.Name))
                        {
                            dto.CustomerName = originalOrder.ApplicationUser.Customer.Name;
                        }
                        else if (originalOrder.UserAddress?.ApplicationUser?.Customer != null && !string.IsNullOrWhiteSpace(originalOrder.UserAddress.ApplicationUser.Customer.Name))
                        {
                            dto.CustomerName = originalOrder.UserAddress.ApplicationUser.Customer.Name;
                        }
                        else if (originalOrder.ApplicationUser != null && !string.IsNullOrWhiteSpace(originalOrder.ApplicationUser.FullName))
                        {
                            dto.CustomerName = originalOrder.ApplicationUser.FullName;
                        }
                    }
                }

                response.Result = mappedData;
                
                // Siparişlere bağlı tüm faturaları çek (LinkedInvoices)
                // İki yönlü ilişki: Invoice.OrderId → siparişe bağlı fatura, Orders.InvoiceId → faturaya bağlı sipariş
                var orderIds = mappedData.Select(o => o.Id).ToList();
                var invoiceIds = mappedData.Where(o => o.InvoiceId.HasValue).Select(o => o.InvoiceId!.Value).Distinct().ToList();
                if (orderIds.Any() || invoiceIds.Any())
                {
                    var linkedInvoices = await uow.DbContext.Set<ecommerce.Core.Entities.Accounting.Invoice>()
                        .AsNoTracking()
                        .Where(inv => inv.Status != (int)EntityStatus.Deleted &&
                            ((inv.OrderId.HasValue && orderIds.Contains(inv.OrderId.Value)) ||
                             invoiceIds.Contains(inv.Id)))
                        .Select(inv => new { inv.OrderId, inv.Id, inv.InvoiceNo, inv.Ettn, inv.IsEInvoice, inv.IsEArchive, inv.EInvoiceStatus })
                        .ToListAsync();
                    
                    foreach (var order in mappedData)
                    {
                        var faturaListesi = linkedInvoices
                            .Where(inv => inv.OrderId == order.Id || inv.Id == order.InvoiceId)
                            .GroupBy(inv => inv.Id)
                            .Select(g => g.First())
                            .Select(inv => new OrderLinkedInvoiceDto
                            {
                                InvoiceId = inv.Id,
                                InvoiceNo = inv.InvoiceNo,
                                Ettn = inv.Ettn,
                                IsEInvoice = inv.IsEInvoice,
                                IsEArchive = inv.IsEArchive,
                                EInvoiceStatus = inv.EInvoiceStatus
                            })
                            .ToList();
                        
                        if (faturaListesi.Any())
                        {
                            order.LinkedInvoices = faturaListesi;
                        }
                    }
                }
                
                 return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPlasiyerCustomersOrders Exception UserId: {UserId}", userId);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        // Fatura oluşturma için sipariş ürünlerini doğrudan çek — Include zinciri sorunlarını bypass eder
        public async Task<List<OrderItems>> GetOrderItemsDirectByOrderIds(List<int> orderIds)
                {
                    try
                    {
                        var distinctIds = orderIds.Distinct().ToList();
                        
                        // Yeni scope kullan — LoadInvoiceFromOrders'da GetOrdersByIds ile aynı DbContext paylaşılıyor
                        // Blazor Server'da scoped DbContext concurrent sorgu hatası verebilir
                        using var scope = _scopeFactory.CreateScope();
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                        
                        // Product Include YOK — Product entity'sindeki HasQueryFilter (BranchId tenant filtresi)
                        // Include sırasında devreye giriyor ve ürünler farklı branch'a aitse tüm OrderItems satırı kayboluyor.
                        // OrderItems entity'sinde ProductName, Price, TotalPrice zaten mevcut — Product'a gerek yok.
                        var items = await uow.DbContext.OrderItems
                            .IgnoreQueryFilters()
                            .AsNoTracking()
                            .Where(oi => distinctIds.Contains(oi.OrderId) && oi.Status != (int)EntityStatus.Deleted)
                            .ToListAsync();

                        _logger.LogInformation("GetOrderItemsDirectByOrderIds — OrderIds: {Ids}, Bulunan item sayısı: {Count}", 
                            string.Join(",", distinctIds), items.Count);

                        return items;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "GetOrderItemsDirectByOrderIds Exception");
                        return new List<OrderItems>();
                    }
                }

        #region Private Helper Methods for Role-Based Filtering

        /// <summary>
        /// Orders için role bazlı filtre uygular
        /// </summary>
        private IQueryable<Orders> ApplyOrderRoleFilter(IQueryable<Orders> query, ApplicationDbContext dbContext)
        {
            var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
            var isB2BAdmin = _tenantProvider.IsB2BAdmin;
            var isPlasiyer = _tenantProvider.IsPlasiyer;
            var isCustomerB2B = _tenantProvider.IsCustomerB2B;
            var currentBranchId = _tenantProvider.GetCurrentBranchId();

            if (isGlobalAdmin)
            {
                return ApplyAdminOrderFilter(query, currentBranchId);
            }
            else if (isB2BAdmin)
            {
                return ApplyB2BAdminOrderFilter(query, dbContext, currentBranchId);
            }
            else if (isPlasiyer)
            {
                return ApplyPlasiyerOrderFilter(query, dbContext);
            }
            else if (isCustomerB2B)
            {
                return ApplyCustomerB2BOrderFilter(query, dbContext);
            }

            return query.Where(x => false);
        }

        private IQueryable<Orders> ApplyAdminOrderFilter(IQueryable<Orders> query, int branchId)
        {
            return branchId > 0
                ? query.Where(x => x.BranchId == branchId)
                : query;
        }

        private IQueryable<Orders> ApplyB2BAdminOrderFilter(IQueryable<Orders> query, ApplicationDbContext dbContext, int currentBranchId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return query.Where(x => false);
            }

            var allowedBranchIds = dbContext.UserBranches
                .AsNoTracking()
                .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                .Select(ub => ub.BranchId)
                .ToList();

            if (!allowedBranchIds.Any())
            {
                return query.Where(x => false);
            }

            // Yetkili tum subelerin siparisleri — dashboard ve liste dolu kalsin
            return query.Where(x => !x.BranchId.HasValue || x.BranchId == 0 || allowedBranchIds.Contains(x.BranchId.Value));
        }

        private IQueryable<Orders> ApplyPlasiyerOrderFilter(IQueryable<Orders> query, ApplicationDbContext dbContext)
        {
            var salesPersonId = _tenantProvider.GetSalesPersonId();

            if (!salesPersonId.HasValue)
            {
                return query.Where(x => false);
            }

            // Plasiyer'e atanan müşterilerin siparişlerini göster
            // Orders -> ApplicationUser -> CustomerId
            var assignedCustomerIds = dbContext.CustomerPlasiyers
                .AsNoTracking()
                .Where(cp => cp.SalesPersonId == salesPersonId.Value)
                .Select(cp => cp.CustomerId)
                .ToList();

            if (!assignedCustomerIds.Any())
            {
                return query.Where(x => false);
            }

            // Also get all UserIds belonging to these customers to catch orders created by them
            // or created on behalf of them (where UserAddress points to them)
            var linkedUserIds = dbContext.Set<ApplicationUser>()
                .AsNoTracking()
                .Where(u => u.CustomerId.HasValue && assignedCustomerIds.Contains(u.CustomerId.Value))
                .Select(u => u.Id)
                .ToList();

            return query.Where(x => 
                // Case 1: Order owner is directly one of the linked customers (via ApplicationUser.CustomerId)
                (x.ApplicationUser != null && x.ApplicationUser.CustomerId.HasValue && assignedCustomerIds.Contains(x.ApplicationUser.CustomerId.Value)) ||
                // Case 2: Order owner (CompanyId) is one of the users belonging to the linked customers
                (linkedUserIds.Contains(x.CompanyId)) ||
                // Case 3: Order address points to one of the users belonging to the linked customers (on behalf)
                (x.UserAddress != null && x.UserAddress.ApplicationUserId.HasValue && linkedUserIds.Contains(x.UserAddress.ApplicationUserId.Value))
            );
        }

        private IQueryable<Orders> ApplyCustomerB2BOrderFilter(IQueryable<Orders> query, ApplicationDbContext dbContext)
        {
            var customerId = _tenantProvider.GetCustomerId();

            if (!customerId.HasValue)
            {
                return query.Where(x => false);
            }

            // Sadece kendi siparişlerini görebilir
            // Hardened: match CompanyId against all users belonging to this CustomerId (via AspNetUsers)
            return query.Where(x => dbContext.AspNetUsers.Any(u => u.Id == x.CompanyId && u.CustomerId == customerId.Value));
        }

        #endregion
    }
}
