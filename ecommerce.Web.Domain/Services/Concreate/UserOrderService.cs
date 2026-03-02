using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Requests;
using ecommerce.Web.Domain.Dtos.Order;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ecommerce.Domain.Shared.Emailing;

namespace ecommerce.Web.Domain.Services.Concreate;

public class UserOrderService : IUserOrderService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPaymentProviderFactory _paymentProviderFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEmailService _emailService;

    public UserOrderService(
        IUnitOfWork<ApplicationDbContext> context, 
        IHttpContextAccessor httpContextAccessor,
        IPaymentProviderFactory paymentProviderFactory,
        IServiceScopeFactory serviceScopeFactory,
        IEmailService emailService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _paymentProviderFactory = paymentProviderFactory;
        _serviceScopeFactory = serviceScopeFactory;
        _emailService = emailService;
    }

    public async Task<UserOrderHistoryDto> GetUserOrderHistoryAsync(OrderStatusType? orderStatus = null, int page = 1, int pageSize = 10)
    {
        var result = new UserOrderHistoryDto
        {
            CurrentPage = page,
            PageSize = pageSize
        };
        
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return result;
  

        // Base query for counts (filtersiz)
        var countQuery = _context.DbContext.Orders
            .Where(o => o.CompanyId == userId && o.Status == (int)EntityStatus.Active);

        // Status counts hesapla
        var statusCounts = await countQuery
            .GroupBy(o => o.OrderStatusType)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var sc in statusCounts)
        {
            result.StatusCounts[sc.Status.ToString()] = sc.Count;
        }

        // Base query for data
        var query = _context.DbContext.Orders
            .Where(o => o.CompanyId == userId && o.Status == (int)EntityStatus.Active);

        // Apply status filter if provided
        if (orderStatus.HasValue)
        {
            query = query.Where(o => o.OrderStatusType == orderStatus.Value);
        }

        // Önce toplam sayıyı al
        var totalCount = await query.CountAsync();
        
        result.TotalCount = totalCount;

        // Projection-based query - fetch only needed data
        var orderProjections = await query
            .OrderByDescending(o => o.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new {
                o.Id,
                o.OrderNumber,
                o.SellerId,
                SellerName = o.Seller != null && !string.IsNullOrWhiteSpace(o.Seller.Name) ? o.Seller.Name.Trim() : "Bilinmeyen Satıcı",
                o.OrderStatusType,
                o.CreatedDate,
                o.ProductTotal,
                o.DiscountTotal,
                o.CargoPrice,
                o.OrderTotal,
                o.GrandTotal,
                o.CargoTrackNumber,
                o.CargoTrackUrl,
                CargoName = o.Cargo != null ? o.Cargo.Name : "",
                o.ShipmentDate,
                // Address info
                AddressId = o.UserAddress != null ? o.UserAddress.Id : 0,
                AddressFullName = o.UserAddress != null ? o.UserAddress.FullName : "",
                Address = o.UserAddress != null ? o.UserAddress.Address : "",
                CityName = o.UserAddress != null && o.UserAddress.City != null ? o.UserAddress.City.Name : "",
                TownName = o.UserAddress != null && o.UserAddress.Town  != null ? o.UserAddress.Town.Name : "",
                AddressPhoneNumber = o.UserAddress != null ? o.UserAddress.PhoneNumber : "",
                // Payment info
                BankId = o.Bank != null ? o.Bank.Id : 0,
                BankName = o.Bank != null ? o.Bank.Name : "",
                BankCardId = o.BankCard != null ? o.BankCard.Id : 0,
                BankCardName = o.BankCard != null ? o.BankCard.Name : "",  // BankCard.Name instead of Last4
                o.PaymentId,
                o.PaymentToken,
                o.Installment,
                o.CardBinNumber,
                o.CardType,
                o.PaymentStatus,
                o.CourierId,
                o.DeliveryOptionType,
                o.EstimatedCourierDeliveryMinutes,
                // Siparişe atanmış araç varsa o aracın şoför adı/plakası; yoksa ana kurye + ilk araç (kargo takip listesinde doğru isim için)
                CourierName = o.CourierVehicleId != null && o.CourierVehicle != null
                    ? (o.CourierVehicle.DriverUser != null
                        ? (o.CourierVehicle.DriverUser.FullName ?? (o.CourierVehicle.DriverUser.FirstName + " " + o.CourierVehicle.DriverUser.LastName).Trim()).Trim()
                        : !string.IsNullOrWhiteSpace(o.CourierVehicle.DriverName) ? o.CourierVehicle.DriverName.Trim() : (o.Courier != null && o.Courier.ApplicationUser != null ? o.Courier.ApplicationUser.FullName : null))
                    : (o.Courier != null && o.Courier.ApplicationUser != null ? o.Courier.ApplicationUser.FullName : null),
                CourierLicensePlate = o.CourierVehicleId != null && o.CourierVehicle != null
                    ? o.CourierVehicle.LicensePlate
                    : (o.Courier != null && o.Courier.Vehicles != null && o.Courier.Vehicles.Any() ? o.Courier.Vehicles.OrderBy(v => v.VehicleType).ThenBy(v => v.LicensePlate).Select(v => v.LicensePlate).FirstOrDefault() : null),
                CourierVehicleType = o.CourierVehicleId != null && o.CourierVehicle != null
                    ? (int?)o.CourierVehicle.VehicleType
                    : (o.Courier != null && o.Courier.Vehicles != null && o.Courier.Vehicles.Any() ? (int?)o.Courier.Vehicles.OrderBy(v => v.VehicleType).ThenBy(v => v.LicensePlate).First().VehicleType : null),
                // Order items
                Items = o.OrderItems.Select(oi => new {
                    oi.Id,
                    oi.ProductId,
                    BrandId = oi.Product != null ? oi.Product.BrandId : 0,
                    oi.ProductName,
                    ProductImage = oi.Product != null && oi.Product.ProductImage.Any() 
                        ? oi.Product.ProductImage.FirstOrDefault().FileName 
                        : "",
                    oi.Quantity,
                    oi.Price,
                    oi.TotalPrice,
                    oi.DiscountAmount,
                    oi.CargoTrackNumber,
                    oi.CargoTrackUrl,
                    oi.CargoExternalId,
                    oi.ShipmentDate,
                    DocumentUrl = oi.Product != null ? oi.Product.DocumentUrl : "",
                    ProductFileGuid = oi.Product != null && oi.Product.ProductImage.Any() 
                        ? oi.Product.ProductImage.FirstOrDefault().FileGuid 
                        : ""
                }).ToList()
            })
            .ToListAsync();

        var grouped = orderProjections.GroupBy(o => o.SellerId);

        foreach (var group in grouped)
        {
            var firstOrder = group.First();
            var sellerGroup = new SellerOrderGroupDto
            {
                SellerId = group.Key,
                SellerName = firstOrder.SellerName,
                Orders = group.Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderStatus = o.OrderStatusType.ToString(),
                    CreatedDate = o.CreatedDate,
                    ProductTotal = o.ProductTotal,
                    DiscountTotal = o.DiscountTotal ?? 0,
                    CargoPrice = o.CargoPrice,
                    OrderTotal = o.OrderTotal,
                    GrandTotal = o.GrandTotal,
                    CargoTrackNumber = o.CargoTrackNumber,
                    CargoTrackUrl = o.CargoTrackUrl,
                    CargoName = o.CargoName,
                    EstimatedDeliveryDate = o.ShipmentDate?.AddDays(3),
                    SellerId = group.Key,
                    SellerName = !string.IsNullOrWhiteSpace(firstOrder.SellerName) ? firstOrder.SellerName : "Bilinmeyen Satıcı",
                    // Reconstruct UserAddress if needed (use ecommerce.Core.Entities types)
                    DeliveryAddress = o.AddressId > 0 ? new ecommerce.Core.Entities.Authentication.UserAddress 
                    {
                        Id = o.AddressId,
                        FullName = o.AddressFullName,
                        Address = o.Address,
                        City = new ecommerce.Core.Entities.City { Name = o.CityName },
                        Town = new ecommerce.Core.Entities.Town { Name = o.TownName },
                        PhoneNumber = o.AddressPhoneNumber
                    } : null,
                    // Bank & Payment Information
                    Bank = o.BankId > 0 ? new ecommerce.Core.Entities.Bank { Id = o.BankId, Name = o.BankName } : null,
                    BankCard = o.BankCardId > 0 ? new ecommerce.Core.Entities.BankCard { Id = o.BankCardId, Name = o.BankCardName } : null,
                    PaymentId = o.PaymentId,
                    PaymentToken = o.PaymentToken,
                    Installment = o.Installment,
                    CardBinNumber = o.CardBinNumber,
                    CardType = o.CardType,
                    OrderStatusType = o.OrderStatusType,
                    PaymentStatus = o.PaymentStatus,
                    CourierId = o.CourierId,
                    CourierName = o.CourierName,
                    CourierLicensePlate = o.CourierLicensePlate,
                    CourierVehicleType = o.CourierVehicleType,
                    DeliveryOptionType = o.DeliveryOptionType.HasValue ? (int?)o.DeliveryOptionType.Value : null,
                    EstimatedCourierDeliveryMinutes = o.EstimatedCourierDeliveryMinutes,
                    Items = o.Items.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        ProductId = oi.ProductId,
                        BrandId = oi.BrandId,
                        ProductName = oi.ProductName,
                        ProductImage = oi.ProductImage,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        TotalPrice = oi.TotalPrice,
                        DiscountAmount = oi.DiscountAmount,
                        CargoTrackNumber = oi.CargoTrackNumber,
                        CargoTrackUrl = oi.CargoTrackUrl,
                        CargoExternalId = oi.CargoExternalId,
                        ShipmentDate = oi.ShipmentDate,
                        DocumentUrl = oi.DocumentUrl,
                        ProductFileGuid = oi.ProductFileGuid
                    }).ToList()
                }).ToList()
            };
            result.SellerGroups.Add(sellerGroup);
        }

        return result;
    }

    public async Task<(bool Success, string Message)> CancelOrder(int orderId, string description)
    {
        try
        {
            Console.WriteLine($"[CancelOrder] Attempting to cancel order ID: {orderId} with description: {description}");
            
            var principal = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return (false, "Yetkilendirme hatası");
            }

            // Create new scope to avoid tracking issues
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                
                var order = await scopedContext.DbContext.Orders
                    .Include(o => o.Bank)
                    .Include(o => o.BankCard)
                    .Include(o => o.User)  // Web context
                    .Include(o => o.ApplicationUser)  // Admin context
                    .Include(o => o.Seller)
                    .Include(o => o.OrderItems).ThenInclude(i => i.ProductImages)
                    .Include(o => o.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.ProductImage)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == userId && o.Status == (int)EntityStatus.Active);

                if (order == null)
                {
                    return (false, "Sipariş bulunamadı");
                }

                // Only allow cancellation for new/pending orders
                if (order.OrderStatusType != OrderStatusType.OrderNew && order.OrderStatusType != OrderStatusType.OrderWaitingPayment)
                {
                    return (false, "Bu sipariş artık iptal edilemez");
                }

                // STEP 1: First, cancel the order status and update cancellation fields
                order.OrderStatusType = OrderStatusType.OrderCanceled;
                order.ReturnOrCancelDate = DateTime.Now;
                order.ReturnOrCancelDescription = description;
                scopedContext.DbContext.Entry(order).State = EntityState.Modified; // Explicitly mark as modified
                await scopedContext.DbContext.SaveChangesAsync(); // SAVE FIRST before calling payment provider
                Console.WriteLine($"[CancelOrder] Order {order.OrderNumber} status changed to OrderCanceled, date and description saved to DB");

                // STEP 2: Then, attempt partial refund if payment was successful
                string refundMessage = "";
                if (order.PaymentStatus && order.Bank != null && !string.IsNullOrEmpty(order.PaymentId))
                {
                    Console.WriteLine($"[CancelOrder] Initiating partial refund for order {order.OrderNumber}, Amount: {order.GrandTotal}");
                    
                    try
                    {
                        var bankParameters = order.Bank.Parameters.ToDictionary(k => k.Key, v => v.Value);
                        
                        // Use SystemName which matches BankNames enum
                        if (!Enum.TryParse<BankNames>(order.Bank.SystemName, out var providerBankName))
                        {
                            Console.WriteLine($"[CancelOrder] Invalid bank SystemName: {order.Bank.SystemName}");
                            refundMessage = " (Banka iadesi yapılamadı - geçersiz banka adı)";
                        }
                        else
                        {
                            var provider = _paymentProviderFactory.Create(providerBankName);
                            
                            // Use REFUND for partial refund (not CANCEL which would cancel entire payment)
                            var refundRequest = new RefundPaymentRequest
                            {
                                OrderNumber = order.OrderNumber,
                                TransactionId = order.PaymentId,
                                ReferenceNumber = order.OrderNumber,
                                TotalAmount = order.GrandTotal, // Only this order's amount
                                Installment = order.Installment ?? 0,
                                CurrencyIsoCode = "TRY",
                                LanguageIsoCode = "tr",
                                CustomerIpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                                BankName = providerBankName,
                                BankParameters = bankParameters
                            };

                            var refundResult = await provider.RefundRequest(refundRequest);
                            
                            if (!refundResult.Success)
                            {
                                Console.WriteLine($"[CancelOrder] Partial refund failed: {refundResult.ErrorMessage}");
                                refundMessage = $" (Banka iadesi başarısız: {refundResult.ErrorMessage})";
                            }
                            else
                            {
                                Console.WriteLine($"[CancelOrder] Partial refund successful for {order.GrandTotal} TL");
                                refundMessage = $" {order.GrandTotal:F2} TL iade işlemi başlatıldı.";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CancelOrder] Refund exception: {ex.Message}");
                        refundMessage = $" (İade sırasında hata: {ex.Message})";
                    }
                }
                
                Console.WriteLine($"[CancelOrder] Order {order.OrderNumber} successfully canceled with refund attempt");

                // STEP 3: Send cancellation emails
                try
                {
                    await _emailService.SendOrderCancelledCustomerEmail(order);
                    await _emailService.SendOrderCancelledSellerEmail(order);
                    Console.WriteLine($"[CancelOrder] Cancellation emails enqueued for {order.OrderNumber}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CancelOrder] Email error: {ex.Message}");
                }

                return (true, $"Sipariş iptal edildi.{refundMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CancelOrder] Exception: {ex.Message}");
            return (false, $"Sipariş iptal edilirken hata oluştu: {ex.Message}");
        }
    }

    public async Task<List<ProductPurchaseHistoryItemDto>> GetProductPurchaseHistoryAsync(int productId)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return new List<ProductPurchaseHistoryItemDto>();

        var items = await _context.DbContext.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ProductId == productId && oi.Orders != null
                && oi.Orders.CompanyId == userId && oi.Orders.Status == (int)EntityStatus.Active
                && oi.Orders.OrderStatusType != OrderStatusType.OrderCanceled)
            .OrderByDescending(oi => oi.Orders!.CreatedDate)
            .Select(oi => new ProductPurchaseHistoryItemDto
            {
                OrderId = oi.OrderId,
                OrderNumber = oi.Orders!.OrderNumber,
                OrderCreatedDate = oi.Orders.CreatedDate,
                Quantity = oi.Quantity,
                UnitPrice = oi.Price,
                TotalPrice = oi.TotalPrice,
                OrderStatusType = oi.Orders.OrderStatusType,
                SellerName = oi.Orders.Seller != null ? oi.Orders.Seller.Name : ""
            })
            .ToListAsync();
        return items;
    }

    public async Task<List<int>> GetPurchasedProductIdsAsync(IEnumerable<int> productIds)
    {
        var ids = productIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0) return new List<int>();

        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return new List<int>();

        var purchased = await _context.DbContext.OrderItems
            .AsNoTracking()
            .Where(oi => ids.Contains(oi.ProductId) && oi.Orders != null
                && oi.Orders.CompanyId == userId && oi.Orders.Status == (int)EntityStatus.Active
                && oi.Orders.OrderStatusType != OrderStatusType.OrderCanceled)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync();
        return purchased;
    }
}
