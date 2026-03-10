using System.Text.Json;
using System.Linq;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils.Threading;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Domain.Shared.Services;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using ecommerce.Domain.Shared.Conts;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace ecommerce.Web.Domain.Services.Concreate;
public class RedisCartService : ICartService{
    private readonly IDatabase _database;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOrderManager _orderManager;
    private readonly ecommerce.Core.Identity.CurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISellerProductService _sellerProductService;
    private readonly ecommerce.Domain.Shared.Abstract.IRealTimeStockResolver _stockResolver;
    private readonly ILogger<RedisCartService> _logger;
    private readonly IConfiguration _configuration;
    private static readonly TimeSpan CartCacheTtl = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan CartLockWaitTimeout = TimeSpan.FromSeconds(15);
    private static readonly JsonSerializerOptions CartCacheSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _userLocks = new();

    public RedisCartService(IConnectionMultiplexer redis, IHttpContextAccessor httpContextAccessor, IOrderManager orderManager, ecommerce.Core.Identity.CurrentUser currentUser, IMapper mapper, IUnitOfWork<ApplicationDbContext> context, IServiceScopeFactory scopeFactory, ISellerProductService sellerProductService, ecommerce.Domain.Shared.Abstract.IRealTimeStockResolver stockResolver, ILogger<RedisCartService> logger, IConfiguration configuration){
        _database = redis.GetDatabase();
        _httpContextAccessor = httpContextAccessor;
        _orderManager = orderManager;
        _currentUser = currentUser;
        _mapper = mapper;
        _context = context;
        _scopeFactory = scopeFactory;
        _sellerProductService = sellerProductService;
        _stockResolver = stockResolver;
        _logger = logger;
        _configuration = configuration;
        _logger.LogInformation($"✅ RedisCartService Active.");
    }
    private string GetUserCartItemKey(int userId, int productSellerItemId) => $"cart:user:{userId}:item:{productSellerItemId}";
    private string GetUserCartItemsSetKey(int userId) => $"cart:user:{userId}:items";
    private string GetUserCartItemMetaKey(int userId, int productSellerItemId) => $"cart:user:{userId}:item:{productSellerItemId}:meta";
    private async Task<int> GetCurrentUserId(){
        // Blazor Context Fallback
        if (_currentUser?.Id != null && _currentUser.Id > 0)
        {
            return _currentUser.Id.Value;
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId)){
            throw new UnauthorizedAccessException("Kullanıcı girişi yapmanız gerekiyor.");
        }
        return userId;
    }
    public async Task<IActionResult<CartDto>> CreateCartItem(CartItemUpsertDto req){
        var rs = OperationResult.CreateResult<CartDto>();
        int userId = 0;
        try{
            userId = await GetCurrentUserId();
        } catch (Exception ex) {
            rs.AddError(ex.Message);
            return rs;
        }

        var sem = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        if (!await sem.WaitAsync(CartLockWaitTimeout))
        {
            _logger.LogWarning($"⚠️ CreateCartItem lock timeout - UserId: {userId}");
            rs.AddError("Sepet işlemi devam ediyor, lütfen kısa süre sonra tekrar deneyin.");
            return rs;
        }

        try{
            _logger.LogInformation($"🛒 CreateCartItem - User: {userId}, ProductSellerItemId: {req.ProductSellerItemId}, Quantity: {req.Quantity}");
            
            // Plasiyer kontrolü — cari seçmeden sepete ürün ekleyemez
            var principal = _httpContextAccessor.HttpContext?.User;
            var salesPersonIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == "SalesPersonId")?.Value;
            
            if (!string.IsNullOrWhiteSpace(salesPersonIdClaim) && int.TryParse(salesPersonIdClaim, out var salesPersonId) && salesPersonId > 0)
            {
                // Kullanıcı Plasiyer — seçili müşteri kontrolü yap
                // Önce DTO'dan gelen CustomerId'yi kontrol et
                if (req.CustomerId == null || req.CustomerId <= 0)
                {
                    _logger.LogWarning($"⚠️ Plasiyer (UserId: {userId}) cari seçmeden sepete ürün eklemeye çalıştı.");
                    rs.AddError("Lütfen önce bir cari seçiniz.");
                    return rs;
                }
                
                _logger.LogInformation($"✅ Plasiyer (UserId: {userId}) seçili cari ile işlem yapıyor: CustomerId={req.CustomerId}");
            }
            
            // ProductSellerItemId bilinmiyorsa ProductId'den otomatik çözümle
            // (Öne çıkan ürünler gibi sipariş geçmişinden gelen ürünler için)
            // Yeni scope açarak izole DbContext kullanıyoruz — concurrent request'lerde tracking çakışmasını önler
            using var resolveScope = _scopeFactory.CreateScope();
            var resolveUow = resolveScope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            
            // ProductId ile eklemede SellerId=1 (genel fiyat listesi) tercih et — ilanlardan satış kaldırıldı
            const int DefaultSellerId = 1;
            if (req.ProductSellerItemId <= 0 && req.ProductId.HasValue && req.ProductId.Value > 0)
            {
                var resolvedSellerItem = await resolveUow.GetRepository<SellerItem>()
                    .GetAll(false).AsNoTracking()
                    .Where(x => x.ProductId == req.ProductId.Value && x.SellerId == DefaultSellerId && x.Status == 1 && x.SalePrice > 0)
                    .OrderByDescending(x => x.Stock)
                    .FirstOrDefaultAsync();
                
                if (resolvedSellerItem != null)
                {
                    req.ProductSellerItemId = resolvedSellerItem.Id;
                    _logger.LogInformation($"🔄 ProductId ({req.ProductId.Value}) → ProductSellerItemId ({resolvedSellerItem.Id}) otomatik çözümlendi (SellerId={DefaultSellerId})");
                }
                else
                {
                    var anySellerItem = await resolveUow.GetRepository<SellerItem>()
                        .GetAll(false).AsNoTracking()
                        .Where(x => x.ProductId == req.ProductId.Value && x.SellerId == DefaultSellerId && x.Status == 1)
                        .FirstOrDefaultAsync();
                    
                    if (anySellerItem != null)
                    {
                        req.ProductSellerItemId = anySellerItem.Id;
                        _logger.LogInformation($"🔄 ProductId ({req.ProductId.Value}) → ProductSellerItemId ({anySellerItem.Id}) otomatik çözümlendi (stok yok, SellerId={DefaultSellerId})");
                    }
                    else
                    {
                        _logger.LogError($"❌ ProductId ({req.ProductId.Value}) için genel fiyat listesinde fiyat tanımlı değil");
                        rs.AddError("Bu ürün için genel fiyat listesinde fiyat tanımlı değil.");
                        return rs;
                    }
                }
            }

            var itemKey = GetUserCartItemKey(userId, req.ProductSellerItemId);
            
            // SellerItem entity'sini yeni scope'daki izole DbContext ile çek — tracking çakışmasını önler
            // IgnoreQueryFilters: Product.BranchId global filter'ı yüzünden başka şubedeki ürün "bulunamadı" olmasın (arama sonucunda çıkan ürün sepete eklenebilsin)
            var sellerItem = await resolveUow.GetRepository<SellerItem>()
                .GetAll(false)
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == req.ProductSellerItemId);
            
            if(sellerItem == null){
                _logger.LogError($"❌ SellerItem bulunamadı - ID: {req.ProductSellerItemId}");
                rs.AddError("Ürün bulunamadı.");
                return rs;
            }
            
            _logger.LogInformation($"✅ SellerItem bulundu - ProductId: {sellerItem.ProductId}, ProductName: {sellerItem.Product?.Name}, SalePrice: {sellerItem.SalePrice}");
            if(sellerItem.Status == 0){
                rs.AddError("Ürün şu anda satışta değildir.");
                return rs;
            }
            var existingQuantity = await _database.StringGetAsync(itemKey);
            var currentQuantity = existingQuantity.HasValue ? (int) existingQuantity : 0;
            var newQuantity = Math.Max(0, currentQuantity + req.Quantity);
            
            // Validate Max
            if(sellerItem.MaxSaleAmount > 0 && newQuantity > sellerItem.MaxSaleAmount){
                rs.AddError($"Bu üründen en fazla {sellerItem.MaxSaleAmount} adet alabilirsiniz.");
                rs.Result = new CartDto();
                return rs;
            }
            
            // If decreasing and result would be below minimum, remove all instead
            if(req.Quantity < 0 && newQuantity > 0 && newQuantity < sellerItem.MinSaleAmount){
                newQuantity = 0;
            }
            
            // Auto-adjust to Min if needed (only when adding, not removing)
            if(req.Quantity > 0 && newQuantity > 0 && newQuantity < sellerItem.MinSaleAmount){
                newQuantity = (int)sellerItem.MinSaleAmount;
            }

            // Stok kontrolü sepete eklerken yapılmıyor — UI'da zaten Elastic'ten gelen stok kullanılıyor (ProductSearch vb.), ekstra satıcı API / DB çağrısı kaldırıldı.
            if(newQuantity == 0){
                await _database.KeyDeleteAsync(itemKey);
                await _database.SetRemoveAsync(GetUserCartItemsSetKey(userId), req.ProductSellerItemId);
                await _database.KeyDeleteAsync(GetUserCartItemMetaKey(userId, req.ProductSellerItemId));
                rs.AddSuccess("Ürün sepetten silindi.");
            } else{
                await _database.StringSetAsync(itemKey, newQuantity);
                await _database.SetAddAsync(GetUserCartItemsSetKey(userId), req.ProductSellerItemId);
                var metaKey = GetUserCartItemMetaKey(userId, req.ProductSellerItemId);
                var metaEntries = new List<HashEntry>
                {
                    new HashEntry("UnitPrice", (double)sellerItem.SalePrice),
                    new HashEntry("SellerId", sellerItem.SellerId),
                    new HashEntry("ProductId", sellerItem.ProductId)
                };
                if (sellerItem.Product?.IsPackageProduct == true)
                {
                    var voucher = !string.IsNullOrWhiteSpace(req.Voucher) ? req.Voucher : (await _database.HashGetAsync(metaKey, "Voucher")).ToString();
                    var guideName = !string.IsNullOrWhiteSpace(req.GuideName) ? req.GuideName : (await _database.HashGetAsync(metaKey, "GuideName")).ToString();
                    if (!string.IsNullOrWhiteSpace(voucher))
                        metaEntries.Add(new HashEntry("Voucher", voucher));
                    if (!string.IsNullOrWhiteSpace(guideName))
                        metaEntries.Add(new HashEntry("GuideName", guideName));
                    if (req.VisitDate.HasValue)
                        metaEntries.Add(new HashEntry("VisitDate", req.VisitDate.Value.ToString("o")));
                    else
                    {
                        var existingVisit = (await _database.HashGetAsync(metaKey, "VisitDate")).ToString();
                        if (!string.IsNullOrWhiteSpace(existingVisit))
                            metaEntries.Add(new HashEntry("VisitDate", existingVisit));
                    }
                    if (req.PackageItemQuantities != null && req.PackageItemQuantities.Count > 0)
                        metaEntries.Add(new HashEntry("PackageItemQuantities", System.Text.Json.JsonSerializer.Serialize(req.PackageItemQuantities)));
                    else
                    {
                        var existingPkg = (await _database.HashGetAsync(metaKey, "PackageItemQuantities")).ToString();
                        if (!string.IsNullOrWhiteSpace(existingPkg))
                            metaEntries.Add(new HashEntry("PackageItemQuantities", existingPkg));
                    }
                }
                await _database.HashSetAsync(metaKey, metaEntries.ToArray());
                rs.AddSuccess("Sepetteki ürün güncellendi.");
            }

            // Cache invalidation — sadece değişen kullanıcının cache'ini sil (performans optimizasyonu)
            await InvalidateCartCaches(userId);
           
            // Tam sepet hesaplaması — güncel sepet bilgilerini döndür
            var refreshed = await GetCart();
            if(!refreshed.Ok){
                rs.AddError("Sepet güncellenemedi.");
                return rs;
            }
            
            rs.Result = refreshed.Result;
            return rs;
        } catch(Exception e){
            _logger.LogError(e, "CreateCartItem Exception");
            rs.AddError(e.Message);
            return rs;
        } finally {
            sem.Release();
        }
    }
    public async Task<IActionResult<CartDto>> GetCart(CartCustomerSavedPreferences? preferences = null){
        var response = OperationResult.CreateResult<CartDto>();
        try{
            var userId = await GetCurrentUserId();

            // OrderManager aynı kullanıcıyı görsün - Sadece eğer asıl kullanıcı (CurrentUser) henüz set edilmemişse HttpContext'ten al
            // Bu sayede Blazor tarafındaki Impersonation (taklit etme) korunmuş olur
            if (_httpContextAccessor.HttpContext?.User != null && (_currentUser.Id == null || _currentUser.Id <= 0))
            {
                 _currentUser.SetUser(_httpContextAccessor.HttpContext.User);
            }

            // Müşteri tercihleri (cookie)
            CartCustomerSavedPreferences ? cartPreferences = preferences;
            if (cartPreferences == null)
            {
                try{
                    var cookies = _httpContextAccessor.HttpContext?.Request?.Cookies;
                    var prefRaw = cookies != null && cookies.ContainsKey(CartConsts.CartPreferencesStorageKey) ? cookies[CartConsts.CartPreferencesStorageKey] : string.Empty;
                    cartPreferences = JsonConvert.DeserializeObject<CartCustomerSavedPreferences>(prefRaw ?? string.Empty);
                } catch{}
            }

            // DB'den sepeti al ve hesaplat (CartService ile aynı yol)
            var execStrategy = _context.DbContext.Database.CreateExecutionStrategy();
            var dbCartItems = await execStrategy.ExecuteAsync(async () => await _orderManager.GetShoppingCart());
            
            var cacheKey = BuildCartCacheKey(userId, dbCartItems, cartPreferences);
            if(!string.IsNullOrEmpty(cacheKey)){
                var cachedPayload = await _database.StringGetAsync(cacheKey);
                if(cachedPayload.HasValue){
                    var cachedCart = System.Text.Json.JsonSerializer.Deserialize<CartDto>(cachedPayload!, CartCacheSerializerOptions);
                    if(cachedCart != null){
                        response.Result = cachedCart;
                        return response;
                    }
                }
            }
            var calculated = await execStrategy.ExecuteAsync(async () => await _orderManager.CalculateShoppingCart(dbCartItems, cartPreferences));
            var cartDto = _mapper.Map<CartDto>(calculated);
            await EnrichCompatibilityAsync(cartDto);
            response.Result = cartDto;
            if(!string.IsNullOrEmpty(cacheKey)){
                var serialized = System.Text.Json.JsonSerializer.Serialize(cartDto, CartCacheSerializerOptions);
                await _database.StringSetAsync(cacheKey, serialized, CartCacheTtl);
            }
          
        } catch(Exception ex){
            response.AddError(ex.ToString());
        }
        return response;
    }
    public async Task<IActionResult<CartDto>> CartItemRemove(int Id){
        var response = OperationResult.CreateResult<CartDto>();
        try{
            var userId = await GetCurrentUserId();
            await _database.KeyDeleteAsync(GetUserCartItemKey(userId, Id));
            await _database.SetRemoveAsync(GetUserCartItemsSetKey(userId), Id);
            // CRITICAL: Also delete meta key to allow re-adding the item
            await _database.KeyDeleteAsync(GetUserCartItemMetaKey(userId, Id));
            
            // Invalidate all cart caches after removing item
            await InvalidateCartCaches(userId);
            
            response.AddSuccess("Ürün sepetten kaldırıldı.");
            try
            {
                var rs = await GetCart();
                if(!rs.Ok) {
                    // Sepet hesaplaması başarısız olsa bile silme işlemi başarılı — boş sepet döndür
                    response.Result = new CartDto();
                    return response;
                }
                rs.Metadata = response.Metadata; // Preserve the success message
                return rs;
            }
            catch (Exception cartEx)
            {
                // Sepet hesaplaması sırasında hata olsa bile silme işlemi başarılı — boş sepet döndür
                _logger.LogWarning(cartEx, "CartItemRemove: Ürün silindi ancak sepet hesaplaması başarısız oldu. UserId: {UserId}, ItemId: {ItemId}", userId, Id);
                response.Result = new CartDto();
                return response;
            }
        } catch(Exception ex){
            response.AddSystemError(ex.ToString());
            return response;
        }
    }
    public async Task<IActionResult<CartDto>> ClearCart(){
        var response = OperationResult.CreateResult<CartDto>();
        try{
            var userId = await GetCurrentUserId();

            var members = await _database.SetMembersAsync(GetUserCartItemsSetKey(userId));
            if (members != null && members.Length > 0){
                var keysToDelete = new List<RedisKey>(members.Length * 2 + 1);
                foreach(var m in members){
                    if(!int.TryParse(m.ToString(), out var psId)) continue;
                    keysToDelete.Add(GetUserCartItemKey(userId, psId));
                    keysToDelete.Add(GetUserCartItemMetaKey(userId, psId));
                }
                keysToDelete.Add(GetUserCartItemsSetKey(userId));
                await _database.KeyDeleteAsync(keysToDelete.ToArray());
            } else {
                await _database.KeyDeleteAsync(GetUserCartItemsSetKey(userId));
            }
            
            // Invalidate all cart caches after clearing cart
            await InvalidateCartCaches(userId);
            
            response.Result = new CartDto();
            response.AddSuccess("ok");
        } catch(Exception ex){
            response.AddSystemError(ex.ToString());
        }
        return response;
    }
    public async Task<IActionResult<CartDto>> PassiveCartItemBySellerId(int sellerId, bool status){
        var response = OperationResult.CreateResult<CartDto>();
        try{
            var userId = await GetCurrentUserId();
            
            // Redis'teki tüm cart item'ları al
            var userItemsSetKey = GetUserCartItemsSetKey(userId);
            var productSellerItemIds = await _database.SetMembersAsync(userItemsSetKey);
            
            if(productSellerItemIds.Any()){
                // Her item'ın meta datasını kontrol et ve seller ID'sine göre status güncelle
                foreach(var psItemId in productSellerItemIds){
                    if(!int.TryParse(psItemId.ToString(), out var productSellerItemId)) continue;
                    
                    var metaKey = GetUserCartItemMetaKey(userId, productSellerItemId);
                    var metaData = await _database.HashGetAllAsync(metaKey);
                    
                    if(metaData.Any()){
                        var sellerIdEntry = metaData.FirstOrDefault(x => x.Name == "SellerId");
                        if(sellerIdEntry.Value.HasValue && (int)sellerIdEntry.Value == sellerId){
                            // Redis meta datasında Status'ü güncelle
                            await _database.HashSetAsync(metaKey, "Status", status ? (int)EntityStatus.Active : (int)EntityStatus.Passive);
                        }
                    }
                }
            }
            
            // Invalidate all cart caches after status change
            await InvalidateCartCaches(userId);
            
            response.AddSuccess(status ? "Satıcı ürünleri aktif edildi." : "Satıcı ürünleri pasif edildi.");
            
            // Güncellenmiş sepeti döndür
            var updatedCart = await GetCart();
            updatedCart.Metadata = response.Metadata;
            return updatedCart;
        } catch(Exception e){
            response.AddSystemError(e.ToString());
            return response;
        }
    }

    public async Task<IActionResult<CartDto>> PassiveCartItemByProductSellerItemId(int productSellerItemId, bool status){
        var response = OperationResult.CreateResult<CartDto>();
        try{
            var userId = await GetCurrentUserId();
            
            // Redis'teki ürünün meta datasını güncelle
            var metaKey = GetUserCartItemMetaKey(userId, productSellerItemId);
            var metaData = await _database.HashGetAllAsync(metaKey);
            
            if(metaData.Any()){
                // Redis meta datasında Status'ü güncelle
                await _database.HashSetAsync(metaKey, "Status", status ? (int)EntityStatus.Active : (int)EntityStatus.Passive);
            }
            
            response.AddSuccess(status ? "Ürün aktif edildi." : "Ürün pasif edildi.");
            
            // Güncellenmiş sepeti döndür
            var updatedCart = await GetCart();
            updatedCart.Metadata = response.Metadata;
            return updatedCart;
        } catch(Exception e){
            response.AddSystemError(e.ToString());
            return response;
        }
    }
    private static int ExtractProductSellerItemId(string key){
        var parts = key.Split(':');
        return int.Parse(parts[^1]);
    }
    private async Task<CartItemDto ?> BuildCartItemDto(int productSellerItemId, int quantity){
        // Yeni scope ile izole DbContext — concurrent çağrılarda tracking çakışmasını önler
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        var sellerItem = await uow.GetRepository<SellerItem>().GetAll(false).AsNoTracking().Include(x => x.Seller).Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == productSellerItemId);
        if(sellerItem == null) return null;
        var pictureGuid = sellerItem.Product?.ProductImage?.FirstOrDefault()?.FileGuid;
        var productName = sellerItem.Product?.Name ?? string.Empty;
        var sellerName = sellerItem.Seller?.Name ?? string.Empty;
        return new CartItemDto{
            Id = sellerItem.Id,
            ProductId = sellerItem.ProductId,
            ProductSellerItemId = sellerItem.Id,
            ProductName = productName,
            UnitPrice = sellerItem.SalePrice,
            Quantity = quantity,
            PictureUrl = pictureGuid,
            SellerId = sellerItem.SellerId,
            SellerName = sellerName,
            Status = 1,
            Step = sellerItem.Step,
            MinSellCount = (int)sellerItem.MinSaleAmount,
            MaxSellCount = (int)sellerItem.MaxSaleAmount,
            IsPackageProduct = sellerItem.Product?.IsPackageProduct ?? false,
            Currency = sellerItem.Currency
        };
    }

    private async Task EnrichCompatibilityAsync(CartDto? cart)
    {
        if(cart?.Sellers == null || cart.Sellers.Count == 0){
            return;
        }

        var allItems = cart.Sellers
            .Where(s => s.Items != null)
            .SelectMany(s => s.Items)
            .Where(i => i != null && i.ProductSellerItemId > 0)
            .ToList();

        if(allItems.Count == 0){
            return;
        }

        var productSellerItemIds = allItems.Select(i => i.ProductSellerItemId).Distinct().ToList();
        var productsResult = await _sellerProductService.GetByIdsAsync(productSellerItemIds);
        
        if(!productsResult.Ok || productsResult.Result == null){
            return;
        }

        var productsDict = productsResult.Result.ToDictionary(p => p.SellerItemId, p => p);

        foreach(var item in allItems){
            if(productsDict.TryGetValue(item.ProductSellerItemId, out var viewModel)){
                if(viewModel?.PerfectCompatibilityCars?.Any() == true){
                    item.IsPerfectCompatibility = true;
                    item.PerfectCompatibilitySummaries = BuildCompatibilitySummaries(viewModel.PerfectCompatibilityCars);
                }
            }
        }
    }

    private static List<string> BuildCompatibilitySummaries(IEnumerable<SellerProductCompatibilityDto> cars)
    {
        var summaries = new List<string>();
        foreach(var car in cars){
            if(car == null){
                continue;
            }

            var parts = new List<string>();
            if(!string.IsNullOrWhiteSpace(car.ManufacturerName)) parts.Add(car.ManufacturerName.Trim());
            if(!string.IsNullOrWhiteSpace(car.BaseModelName)) parts.Add(car.BaseModelName.Trim());
            if(!string.IsNullOrWhiteSpace(car.SubModelName)) parts.Add(car.SubModelName.Trim());
            if(!string.IsNullOrWhiteSpace(car.PlateNumber)) parts.Add($"({car.PlateNumber.Trim()})");

            if(parts.Count > 0){
                summaries.Add(string.Join(" ", parts));
            }
        }

        return summaries;
    }

    private static string BuildCartCacheKey(int userId, List<CartItem> cartItems, CartCustomerSavedPreferences? preferences)
    {
        var builder = new StringBuilder();
        builder.Append(userId).Append('|');
        foreach(var item in cartItems.OrderBy(i => i.ProductSellerItemId)){
            builder.Append(item.ProductSellerItemId)
                   .Append(':')
                   .Append(item.Quantity)
                   .Append(':')
                   .Append(item.Status);
            if (item.Product?.IsPackageProduct == true)
            {
                if (item.VisitDate.HasValue)
                    builder.Append(':').Append(item.VisitDate.Value.ToString("o"));
                if (item.PackageItemQuantities != null && item.PackageItemQuantities.Count > 0)
                    builder.Append(':').Append(System.Text.Json.JsonSerializer.Serialize(item.PackageItemQuantities.OrderBy(x => x.Key)));
            }
            builder.Append(';');
        }
        builder.Append("coupon=").Append(preferences?.UsedCouponCode ?? string.Empty).Append('|');
        if(preferences?.SelectedCargoes?.Any() == true){
            foreach(var kvp in preferences.SelectedCargoes.OrderBy(k => k.Key)){
                builder.Append("cg")
                       .Append(kvp.Key)
                       .Append('=')
                       .Append(kvp.Value)
                       .Append(';');
            }
        }
        var raw = builder.ToString();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return $"cart:result:{userId}:{hash}";
    }

    private async Task InvalidateCartCaches(int userId)
    {
        try
        {
            // SCAN kullan — KEYS komutu production'da Redis'i bloklar
            var pattern = $"cart:result:{userId}:*";
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var keys = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 100))
            {
                keys.Add(key);
            }
            if (keys.Count > 0)
            {
                await _database.KeyDeleteAsync(keys.ToArray());
                _logger.LogInformation($"🗑️ {keys.Count} sepet cache'i silindi — UserId: {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sepet cache temizleme başarısız");
        }
    }
    
    // Sepetteki toplam ürün sayısını hızlıca hesapla (batch Redis sorgusu — N+1 yok)
    private async Task<int> GetCartItemCount(int userId)
    {
        try
        {
            var members = await _database.SetMembersAsync(GetUserCartItemsSetKey(userId));
            if (members == null || members.Length == 0) return 0;
            
            // Tüm miktar anahtarlarını tek seferde oku (batch)
            var keys = members
                .Where(m => int.TryParse(m.ToString(), out _))
                .Select(m => (RedisKey)GetUserCartItemKey(userId, int.Parse(m.ToString())))
                .ToArray();
            
            if (keys.Length == 0) return 0;
            
            var values = await _database.StringGetAsync(keys);
            int totalCount = 0;
            foreach (var val in values)
            {
                if (val.HasValue) totalCount += (int)val;
            }
            return totalCount;
        }
        catch
        {
            return 0;
        }
    }
    
    private async Task<IActionResult<Empty>> CheckRealTimeStockAsync(SellerItem sellerItem, int desiredQuantity)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        
        string? lookupKey = sellerItem.Product?.Barcode;
        int dbStockFallback = (int)sellerItem.Stock; // Use SellerItem.Stock as primary fallback
        
        _logger.LogInformation($"🚨 [CHECK] SellerId: {sellerItem.SellerId}, SourceId: {sellerItem.SourceId}, ProductId: {sellerItem.ProductId}, Barcode: {lookupKey}, SellerItemStock: {dbStockFallback}, Desired: {desiredQuantity}");
        
        // IsApiStockCheck false: satıcı API'sine bakma. SkipDbStockCheck true: DB'ye de bakma (Elastic stok yeterli).
        if (!_configuration.GetValue<bool>("IsApiStockCheck", true))
        {
            if (_configuration.GetValue<bool>("SkipDbStockCheck", false))
            {
                _logger.LogInformation($"✅ [Elastic] Stock OK (No API, No DB check).");
                return rs;
            }
            if (dbStockFallback < desiredQuantity)
            {
                _logger.LogWarning($"❌ [SellerItem] Insufficient Stock. DB: {dbStockFallback}, Req: {desiredQuantity}");
                rs.AddError($"Depoda yeterli stok bulunmamaktadır. Mevcut: {dbStockFallback}");
                return rs;
            }
            _logger.LogInformation($"✅ [SellerItem] Stock OK (No API Check). DB: {dbStockFallback}");
            return rs;
        }

        if (sellerItem.SellerId <= 0)
        {
            return rs;
        }

        try 
        {
            // API Check (if provider available) - use Product.Barcode for lookup
            var stockProvider = _stockResolver.GetProvider(sellerItem.SellerId);
            if (stockProvider != null)
            {
                 if (string.IsNullOrEmpty(lookupKey))
                 {
                      _logger.LogWarning($"⚠️ [{sellerItem.SellerId}] lookupKey is null/empty. Using SellerItem.Stock fallback: {dbStockFallback}");
                      // Fallback to SellerItem.Stock
                      if (dbStockFallback < desiredQuantity)
                      {
                          rs.AddError($"Depoda yeterli stok bulunmamaktadır. Mevcut: {dbStockFallback}");
                          return rs;
                      }
                      return rs;
                 }

                 _logger.LogInformation($"🔍 [{sellerItem.SellerId}] Checking API Stock for Key: {lookupKey}, SourceId: {sellerItem.SourceId}");
                 
                 string stockInfo = string.Empty;
                 try
                 {
                     stockInfo = await stockProvider.GetStockAsync(lookupKey, sellerItem.SourceId).WaitAsync(TimeSpan.FromSeconds(3));
                 }
                 catch (TimeoutException)
                 {
                     _logger.LogWarning($"⚠️ [{sellerItem.SellerId}] API Stock Check timed out after 3s. Using SellerItem.Stock fallback: {dbStockFallback}");
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError(ex, $"⚠️ [{sellerItem.SellerId}] API Stock Check failed. Using SellerItem.Stock fallback: {dbStockFallback}");
                 }
                 
                 _logger.LogInformation($"📦 [{sellerItem.SellerId}] API Response: {stockInfo}");

                 // Simple API response handling
                 if (string.IsNullOrEmpty(stockInfo))
                 {
                     // API returned empty - use SellerItem.Stock
                     _logger.LogWarning($"⚠️ API empty response. Using SellerItem.Stock: {dbStockFallback}");
                     if (dbStockFallback < desiredQuantity)
                     {
                         rs.AddError($"Depoda yeterli stok bulunmamaktadır. Mevcut: {dbStockFallback}");
                         return rs;
                     }
                     return rs;
                 }
                 
                 // API says "Stok Yok"
                 if (stockInfo.Contains("Stok Yok", StringComparison.OrdinalIgnoreCase))
                 {
                     if (dbStockFallback >= desiredQuantity)
                     {
                         _logger.LogInformation($"✅ API: No stock, but SellerItem.Stock OK: {dbStockFallback}");
                         return rs;
                     }
                     _logger.LogWarning($"❌ API: Stok Yok, SellerItem.Stock also insufficient: {dbStockFallback}");
                     rs.AddError($"Depoda stok tükenmiştir.");
                     return rs;
                 }
                 
                // API says "VAR" (stock available) or other positive response
                // Still need to validate against SellerItem.Stock
                if (dbStockFallback < desiredQuantity)
                {
                    _logger.LogWarning($"⚠️ API: Stock available, but SellerItem.Stock insufficient. DB: {dbStockFallback}, Req: {desiredQuantity}");
                    rs.AddError($"Depoda yeterli stok bulunmamaktadır. Mevcut: {dbStockFallback}");
                    return rs;
                }
                _logger.LogInformation($"✅ API: Stock available, SellerItem.Stock OK: {dbStockFallback}");
                return rs;
            }
            else 
            {
                 // No API provider - Use SellerItem.Stock only
                 _logger.LogInformation($"ℹ️ [{sellerItem.SellerId}] No API provider. Using SellerItem.Stock: {dbStockFallback}");
                 if (dbStockFallback < desiredQuantity)
                 {
                     rs.AddError($"Depoda yeterli stok bulunmamaktadır. Mevcut: {dbStockFallback}");
                     return rs;
                 }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckRealTimeStockAsync Error");
            
            // Always fallback to SellerItem.Stock on exception
            if (dbStockFallback < desiredQuantity)
            {
                 _logger.LogWarning($"❌ [Exception] SellerItem.Stock Insufficient. DB: {dbStockFallback}, Req: {desiredQuantity}");
                 rs.AddError($"Depoda yeterli stok bulunmamaktadır. Mevcut: {dbStockFallback}");
                 return rs;
            }
            else
            {
                 _logger.LogInformation($"✅ [Exception] Using SellerItem.Stock fallback: {dbStockFallback}");
                 return rs; 
            }
        }

        return rs;
    }
}
