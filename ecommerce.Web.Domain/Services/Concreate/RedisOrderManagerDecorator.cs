using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Services;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ecommerce.Web.Domain.Services.Concreate;

using ecommerce.Core.Interfaces;

public class RedisOrderManagerDecorator : IOrderManager
{
    private readonly IOrderManager _inner;
    private readonly IDatabase _database;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ITenantProvider _tenantProvider;
    private readonly ecommerce.Core.Identity.CurrentUser _currentUser;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> _userLocks = new();
    
    // DbContextOptions'ı bir kez oluştur, her GetShoppingCart çağrısında tekrar parse etme
    private static DbContextOptions<ApplicationDbContext>? _cachedCartDbOptions;
    private static readonly object _optionsLock = new();

    public RedisOrderManagerDecorator(IOrderManager inner, IConnectionMultiplexer redis, IUnitOfWork<ApplicationDbContext> context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ITenantProvider tenantProvider, ecommerce.Core.Identity.CurrentUser currentUser)
    {
        _inner = inner;
        _database = redis.GetDatabase();
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
    }

    private async Task<int> GetCurrentUserId()
    {
        // Blazor Context Fallback (same as RedisCartService)
        if (_currentUser?.Id != null && _currentUser.Id > 0)
        {
            return _currentUser.Id.Value;
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return 0;
        }
        return userId;
    }

    private static string CartSetKey(int userId) => $"cart:user:{userId}:items";
    private static string CartItemKey(int userId, int psId) => $"cart:user:{userId}:item:{psId}";
    private static string CartItemMetaKey(int userId, int psId) => $"cart:user:{userId}:item:{psId}:meta";

    public async Task<List<CartItem>> GetShoppingCart()
    {
		var userId = await GetCurrentUserId();
        Console.WriteLine($"[RedisOrderManagerDecorator] GetShoppingCart - UserId: {userId}");
		if (userId <= 0) return new List<CartItem>();

		// No lock needed here - Redis operations are atomic and this is read-only
		// CreateCartItem in RedisCartService already handles locking for writes

		var members = await _database.SetMembersAsync(CartSetKey(userId));
        Console.WriteLine($"[RedisOrderManagerDecorator] Redis Key: {CartSetKey(userId)}, Members Count: {members?.Length ?? 0}");
		if (members == null || members.Length == 0) return new List<CartItem>();

		var productSellerItemIds = new List<int>(members.Length);
		foreach (var member in members)
		{
			if (int.TryParse(member.ToString(), out var psId))
			{
				productSellerItemIds.Add(psId);
			}
		}
		if (productSellerItemIds.Count == 0) return new List<CartItem>();

	
		var qtyKeys = productSellerItemIds
			.Select(psId => (RedisKey)CartItemKey(userId, psId))
			.ToArray();
		var qtyValues = await _database.StringGetAsync(qtyKeys);
		var quantitiesByPsId = new Dictionary<int, int>(productSellerItemIds.Count);
		for (var i = 0; i < productSellerItemIds.Count; i++)
		{
			var val = qtyValues[i];
			if (!val.HasValue) continue;
			var quantity = (int)val;
			if (quantity <= 0) continue;
			quantitiesByPsId[productSellerItemIds[i]] = quantity;
		}
		if (quantitiesByPsId.Count == 0) return new List<CartItem>();

        // 3) Tüm SellerItem'ları tek EF sorgusuyla çek — cached DbContextOptions kullan
        var ids = quantitiesByPsId.Keys.ToList();
        
        if (_cachedCartDbOptions == null)
        {
            lock (_optionsLock)
            {
                if (_cachedCartDbOptions == null)
                {
                    var rawCs = _configuration.GetConnectionString("ApplicationDbContext");
                    var csb = new NpgsqlConnectionStringBuilder(rawCs)
                    {
                        KeepAlive = 30,
                        MaxPoolSize = 200,
                        Multiplexing = false
                    };
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder
                        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                        .UseNpgsql(csb.ConnectionString, o =>
                        {
                            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                             .MigrationsAssembly("ecommerce.EFCore");
                            o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null);
                            o.CommandTimeout(60);
                        });
                    _cachedCartDbOptions = optionsBuilder.Options;
                }
            }
        }
        
        await using var db = new ApplicationDbContext(_cachedCartDbOptions, _tenantProvider);
        var sellerItems = await db.Set<SellerItem>()
			.AsNoTracking()
			.Include(x => x.Product)
			    .ThenInclude(p => p.ProductImage)
			.Include(x => x.Product)
			    .ThenInclude(p => p.Categories)
			.Include(x => x.Seller)
			.Where(x => ids.Contains(x.Id))
			.ToListAsync();
		var sellerItemById = sellerItems.ToDictionary(x => x.Id, x => x);

		// 4) Redis'ten Status bilgilerini BATCH olarak al (N+1 problemi çözümü)
		var statusByPsId = new Dictionary<int, int>(productSellerItemIds.Count);
		
		// Use Redis BATCH/PIPELINE to get all statuses in a single round-trip
		var batch = _database.CreateBatch();
		var statusTasks = new Dictionary<int, Task<HashEntry[]>>(productSellerItemIds.Count);
		
		foreach (var psId in productSellerItemIds)
		{
			var metaKey = CartItemMetaKey(userId, psId);
			statusTasks[psId] = batch.HashGetAllAsync(metaKey);
		}
		
		batch.Execute();
		
		// Wait for all tasks and process results
		foreach (var (psId, task) in statusTasks)
		{
			try
			{
				var metaData = await task;
				var statusEntry = metaData.FirstOrDefault(x => x.Name == "Status");
				statusByPsId[psId] = statusEntry.Value.HasValue 
					? (int)statusEntry.Value 
					: (int)EntityStatus.Active;
			}
			catch
			{
				// Status yoksa Active kabul et
				statusByPsId[psId] = (int)EntityStatus.Active;
			}
		}

		// 5) Project to CartItem objects (tüm item'lar, Status bilgisiyle)
		var result = new List<CartItem>(sellerItemById.Count);
		
		foreach (var (psId, quantity) in quantitiesByPsId)
		{
			if (!sellerItemById.TryGetValue(psId, out var sellerItem)) continue;
			
			// Status al (yoksa Active kabul et)
			if(!statusByPsId.TryGetValue(psId, out var itemStatus)){
				itemStatus = (int)EntityStatus.Active;
			}
			
			result.Add(new CartItem
			{
				Id = sellerItem.Id,
				UserId = userId,
				ProductId = sellerItem.ProductId,
				ProductSellerItemId = sellerItem.Id,
				Quantity = quantity,
				Status = itemStatus, // Status bilgisini koru (Active veya Passive)
				Product = sellerItem.Product!,
				ProductSellerItem = sellerItem,
				User = new ecommerce.Core.Entities.Authentication.User { Id = userId }
			});
		}

		return result;
    }

    public async Task ClearShoppingCart(int companyId, int? sellerId = null)
    {
        // 1. Clear Inner (Database)
        await _inner.ClearShoppingCart(companyId, sellerId);

        // 2. Clear Redis
        if (companyId > 0)
        {
            var cartSetKey = CartSetKey(companyId);
            var members = await _database.SetMembersAsync(cartSetKey);
            
            if (members != null && members.Length > 0)
            {
                var transaction = _database.CreateTransaction();
                foreach (var member in members)
                {
                    if (int.TryParse(member.ToString(), out var psId))
                    {
                        transaction.KeyDeleteAsync(CartItemKey(companyId, psId));
                        transaction.KeyDeleteAsync(CartItemMetaKey(companyId, psId));
                    }
                }
                transaction.KeyDeleteAsync(cartSetKey);
                await transaction.ExecuteAsync();
            }
        }
    }
    public Task<CartResult> CalculateShoppingCart(List<CartItem> cart, CartCustomerSavedPreferences? cartPreferences = null, bool includeCommissions = false) => _inner.CalculateShoppingCart(cart, cartPreferences, includeCommissions);
    public Task<List<Discount>> GetAllAllowedDiscountsAsync(List<CartItem> cart, string? couponCode = null) => _inner.GetAllAllowedDiscountsAsync(cart, couponCode);
    public Task<bool> ValidateDiscountWithRuleAsync(Discount discount) => _inner.ValidateDiscountWithRuleAsync(discount);
    public List<Discount> GetPreferredDiscount(IList<Discount> discounts, decimal amount, out decimal discountAmount) => _inner.GetPreferredDiscount(discounts, amount, out discountAmount);
    public decimal GetDiscountAmount(Discount discount, decimal amount) => _inner.GetDiscountAmount(discount, amount);
    public Task<decimal> GetAppMinimumCartTotal() => _inner.GetAppMinimumCartTotal();
}


