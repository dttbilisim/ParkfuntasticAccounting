using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Domain.Dtos.Order;
using ecommerce.Web.Domain.Dtos.Address;
using ecommerce.Web.Domain.Dtos.Bank;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Interfaces;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.EP.Controllers
{
    /// <summary>
    /// Cart and Checkout Management Controller
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ICheckoutService _checkoutService;
        private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
        private readonly IBankService _bankService;
        private readonly ICourierNotificationService? _courierNotificationService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            ICheckoutService checkoutService,
            IUnitOfWork<ApplicationDbContext> unitOfWork,
            IBankService bankService,
            ILogger<CartController> logger,
            ICourierNotificationService? courierNotificationService = null)
        {
            _cartService = cartService;
            _checkoutService = checkoutService;
            _unitOfWork = unitOfWork;
            _bankService = bankService;
            _logger = logger;
            _courierNotificationService = courierNotificationService;
        }

        /// <summary>
        /// Gets the current user's basket.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartDto))]
        public async Task<IActionResult> GetCart([FromQuery] CartCustomerSavedPreferences? preferences = null)
        {
            // Debug: Kullanıcı claim'lerini logla
            var claims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            _logger.LogInformation("GetCart çağrıldı. Claims: {Claims}", string.Join(", ", claims));
            
            // Debug: UserId kontrolü
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("GetCart UserId claim: {UserId}, IsAuthenticated: {IsAuth}", 
                userIdClaim ?? "NULL", User.Identity?.IsAuthenticated);
            
            var result = await _cartService.GetCart(preferences);
            
            // Debug: Sonucu logla
            _logger.LogInformation("GetCart sonuç: Ok={Ok}, TotalItems={TotalItems}, SellerCount={SellerCount}", 
                result.Ok, result.Result?.TotalItems ?? -1, result.Result?.Sellers?.Count ?? -1);
            
            if (result.Ok)
            {
                return Ok(result.Result);
            }
            
            var errorMsg = result.Metadata?.Message ?? "Sepet bilgileri alınamadı.";
            _logger.LogError("GetCart başarısız. MetadataType: {Type}, Hata: {Error}", 
                result.Metadata?.Type.ToString() ?? "null",
                errorMsg.Length > 500 ? errorMsg.Substring(0, 500) : errorMsg);
            return BadRequest(new { message = errorMsg });
        }

        /// <summary>
        /// Adds a product to the cart or updates its quantity.
        /// </summary>
        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartDto))]
        public async Task<IActionResult> AddToCart([FromBody] CartItemUpsertDto request)
        {
            var result = await _cartService.CreateCartItem(request);
            if (result.Ok)
            {
                return Ok(result.Result);
            }
            return BadRequest(result.Metadata?.Message ?? "Ürün sepete eklenemedi.");
        }

        /// <summary>
        /// Removes an item from the cart.
        /// </summary>
        [HttpPost("remove/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartDto))]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var result = await _cartService.CartItemRemove(id);
            if (result.Ok)
            {
                return Ok(result.Result);
            }
            return BadRequest(result.Metadata?.Message ?? "Ürün sepetten çıkarılamadı.");
        }

        /// <summary>
        /// Clears the entire cart.
        /// </summary>
        [HttpPost("clear")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartDto))]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _cartService.ClearCart();
            if (result.Ok)
            {
                return Ok(result.Result);
            }
            return BadRequest(result.Metadata?.Message ?? "Sepet temizlenemedi.");
        }

        /// <summary>
        /// Toggles the active status of a specific item in the cart.
        /// </summary>
        [HttpPost("toggle-item")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartDto))]
        public async Task<IActionResult> ToggleItem([FromBody] ToggleItemRequest request)
        {
            var result = await _cartService.PassiveCartItemByProductSellerItemId(request.ProductSellerItemId, request.IsActive);
            if (result.Ok)
            {
                return Ok(result.Result);
            }
            return BadRequest(result.Metadata?.Message ?? "Ürün durumu güncellenemedi.");
        }

        /// <summary>
        /// Toggles the active status of all items from a specific seller.
        /// </summary>
        [HttpPost("toggle-seller")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CartDto))]
        public async Task<IActionResult> ToggleSeller([FromBody] ToggleSellerRequest request)
        {
            var result = await _cartService.PassiveCartItemBySellerId(request.SellerId, request.IsActive);
            if (result.Ok)
            {
                return Ok(result.Result);
            }
            return BadRequest(result.Metadata?.Message ?? "Satıcı ürünleri durumu güncellenemedi.");
        }

        /// <summary>
        /// Gets the active banks for online payment.
        /// </summary>
        [HttpGet("banks")]
        public async Task<IActionResult> GetBanks()
        {
            var result = await _bankService.GetActiveBanksAsync();
            if (result.Ok) return Ok(result.Result);
            return BadRequest(result.Metadata?.Message ?? "Banka bilgileri alınamadı.");
        }

        /// <summary>
        /// Gets card types for a specific bank.
        /// </summary>
        [HttpGet("banks/{bankId}/cards")]
        public async Task<IActionResult> GetBankCards(int bankId)
        {
            var result = await _bankService.GetBankCardsAsync(bankId);
            if (result.Ok) return Ok(result.Result);
            return BadRequest(result.Metadata?.Message ?? "Kart bilgileri alınamadı.");
        }

        /// <summary>
        /// Gets installment options for a specific card type.
        /// </summary>
        [HttpGet("cards/{cardId}/installments")]
        public async Task<IActionResult> GetInstallments(int cardId)
        {
            var result = await _bankService.GetBankInstallmentsAsync(cardId);
            if (result.Ok) return Ok(result.Result);
            return BadRequest(result.Metadata?.Message ?? "Taksit bilgileri alınamadı.");
        }

        /// <summary>
        /// Gets the addresses for the current customer.
        /// Plasiyer ise müşterinin adreslerini, normal kullanıcı ise kendi adreslerini getirir.
        /// </summary>
        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses([FromQuery] int? customerId = null)
        {
            // Kullanıcının ApplicationUser.Id'sini al
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var applicationUserId))
            {
                return BadRequest("Kullanıcı kimliği belirlenemedi.");
            }

            // ApplicationUser'ı yükle — CustomerId ve SalesPersonId kontrolü için
            var applicationUser = await _unitOfWork.GetRepository<ApplicationUser>()
                .GetAll(predicate: u => u.Id == applicationUserId, disableTracking: true)
                .Select(u => new { u.CustomerId, u.SalesPersonId })
                .FirstOrDefaultAsync();

            // Hedef müşteri ID'sini belirle
            int? targetCustomerId = applicationUser?.CustomerId;

            // Plasiyer senaryosu: customerId parametresi veya SalesPersonId üzerinden müşteri bul
            if (applicationUser?.SalesPersonId.HasValue == true)
            {
                if (customerId.HasValue)
                {
                    // Plasiyerin bu müşteriye yetkisi var mı kontrol et
                    var isLinked = await _unitOfWork.DbContext.CustomerPlasiyers
                        .AsNoTracking()
                        .AnyAsync(cp => cp.SalesPersonId == applicationUser.SalesPersonId.Value 
                                     && cp.CustomerId == customerId.Value);
                    if (isLinked)
                    {
                        targetCustomerId = customerId.Value;
                    }
                }
                else if (!targetCustomerId.HasValue)
                {
                    // Plasiyerin ilk bağlı müşterisini bul (fallback)
                    var firstCustomerId = await _unitOfWork.DbContext.CustomerPlasiyers
                        .AsNoTracking()
                        .Where(cp => cp.SalesPersonId == applicationUser.SalesPersonId.Value)
                        .Select(cp => cp.CustomerId)
                        .FirstOrDefaultAsync();
                    if (firstCustomerId > 0)
                    {
                        targetCustomerId = firstCustomerId;
                    }
                }
            }

            // Adres sorgusu için ApplicationUser ID listesini belirle
            List<int> targetUserIds;
            if (targetCustomerId.HasValue)
            {
                // Müşteriye bağlı tüm ApplicationUser'ların adreslerini getir
                targetUserIds = await _unitOfWork.GetRepository<ApplicationUser>()
                    .GetAll(predicate: u => u.CustomerId == targetCustomerId.Value, disableTracking: true)
                    .Select(u => u.Id)
                    .ToListAsync();
            }
            else
            {
                // Normal kullanıcı — kendi adresleri
                targetUserIds = new List<int> { applicationUserId };
            }

            var addresses = await _unitOfWork.GetRepository<UserAddress>()
                .GetAll(predicate: null)
                .Include(a => a.City)
                .Include(a => a.Town)
                .Where(a => a.ApplicationUserId.HasValue && targetUserIds.Contains(a.ApplicationUserId.Value))
                .Select(a => new
                {
                    a.Id,
                    UserId = a.ApplicationUserId,
                    Title = a.AddressName,
                    FirstName = a.FullName,
                    Phone = a.PhoneNumber,
                    City = a.City != null ? a.City.Name : null,
                    CityId = a.CityId,
                    Town = a.Town != null ? a.Town.Name : null,
                    TownId = a.TownId,
                    AddressText = a.Address,
                    a.IsDefault,
                })
                .ToListAsync();

            // Adres bulunamazsa ve müşteri varsa, müşteri bilgilerinden otomatik adres oluştur
            if (!addresses.Any() && targetCustomerId.HasValue)
            {
                var customer = await _unitOfWork.GetRepository<ecommerce.Core.Entities.Accounting.Customer>()
                    .GetAll(predicate: c => c.Id == targetCustomerId.Value, disableTracking: false)
                    .Include(c => c.City)
                    .Include(c => c.Town)
                    .FirstOrDefaultAsync();

                if (customer != null && !string.IsNullOrWhiteSpace(customer.Address))
                {
                    // Müşteri bilgilerinden kalıcı adres oluştur
                    var ownerUserId = targetUserIds.FirstOrDefault();
                    if (ownerUserId == 0) ownerUserId = applicationUserId;

                    var newAddress = new UserAddress
                    {
                        AddressName = "Sistem Kayıtlı Adres",
                        FullName = customer.Name,
                        Email = customer.Email ?? "",
                        PhoneNumber = customer.Mobile ?? customer.Phone ?? "",
                        Address = customer.Address,
                        CityId = customer.CityId,
                        TownId = customer.TownId,
                        ApplicationUserId = ownerUserId,
                        IsDefault = true,
                        Status = (int)ecommerce.Core.Utils.EntityStatus.Active,
                        CreatedDate = DateTime.Now,
                        CreatedId = applicationUserId
                    };

                    _unitOfWork.GetRepository<UserAddress>().Insert(newAddress);
                    await _unitOfWork.SaveChangesAsync();

                    // Yeni oluşturulan adresi döndür
                    return Ok(new[]
                    {
                        new
                        {
                            newAddress.Id,
                            UserId = (int?)newAddress.ApplicationUserId,
                            Title = newAddress.AddressName,
                            FirstName = newAddress.FullName,
                            Phone = newAddress.PhoneNumber,
                            City = customer.City?.Name,
                            CityId = newAddress.CityId,
                            Town = customer.Town?.Name,
                            TownId = newAddress.TownId,
                            AddressText = newAddress.Address,
                            newAddress.IsDefault,
                        }
                    });
                }
            }

            return Ok(addresses);
        }

        /// <summary>
        /// Completes the order checkout.
        /// </summary>
        [HttpPost("checkout")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CheckoutResultDto))]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            // If the user is a Plasiyer, they might be placing an order on behalf of a customer.
            // AuthController adds "SalesPersonId" claim for Plasiyers.
            // Impersonation logic is handled inside CheckoutService.
            
            var result = await _checkoutService.Checkout(request);
            if (result.Ok && result.Result != null)
            {
                if (_courierNotificationService != null && result.Result.OrderIds != null && result.Result.OrderIds.Count > 0)
                {
                    var ordersWithCourier = await _unitOfWork.DbContext.Orders
                        .AsNoTracking()
                        .Include(o => o.Courier)
                        .Where(o => result.Result.OrderIds.Contains(o.Id) && o.CourierId != null && o.Courier != null)
                        .ToListAsync();
                    foreach (var order in ordersWithCourier)
                    {
                        try
                        {
                            await _courierNotificationService.SendOrderAssignedNotificationAsync(
                                order.Courier!.ApplicationUserId,
                                order.OrderNumber ?? $"#{order.Id}",
                                order.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "Kurye push bildirimi gönderilirken hata (sipariş #{OrderId}, kurye UserId: {CourierUserId}). Bildirim listeye yazıldı.",
                                order.Id, order.Courier!.ApplicationUserId);
                        }
                    }
                }
                return Ok(result.Result);
            }
            return BadRequest(result.Metadata?.Message ?? "Sipariş tamamlanamadı.");
        }
    }

    public class ToggleItemRequest
    {
        public int ProductSellerItemId { get; set; }
        public bool IsActive { get; set; }
    }

    public class ToggleSellerRequest
    {
        public int SellerId { get; set; }
        public bool IsActive { get; set; }
    }
}
