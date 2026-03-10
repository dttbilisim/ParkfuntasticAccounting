using System.Runtime.CompilerServices;
using AutoMapper;
using System.Collections.Concurrent;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Domain.Shared.Services;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
namespace ecommerce.Web.Domain.Services.Concreate;
public class CartService : ICartService{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOrderManager _orderManager;
    private readonly ecommerce.Core.Identity.CurrentUser _currentUser;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _userLocks = new();
    public CartService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IOrderManager orderManager, ecommerce.Core.Identity.CurrentUser currentUser, IServiceScopeFactory scopeFactory){
        _context = context;
        _mapper = mapper;
        _orderManager = orderManager;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }
    public async Task<IActionResult<CartDto>> CreateCartItem(CartItemUpsertDto req){
        var rs = OperationResult.CreateResult<CartDto>();
        try{
            var principal = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId)){
                rs.AddError("Kullanıcı girişi yapmanız gerekiyor.");
                return rs;
            }
            // Plasiyer kontrolü — cari seçmeden sepete ürün ekleyemez
            var salesPersonIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == "SalesPersonId")?.Value;
            if (!string.IsNullOrWhiteSpace(salesPersonIdClaim) && int.TryParse(salesPersonIdClaim, out var salesPersonId) && salesPersonId > 0)
            {
                if (req.CustomerId == null || req.CustomerId <= 0)
                {
                    rs.AddError("Lütfen önce bir cari seçiniz.");
                    return rs;
                }
            }
            // ProductSellerItemId bilinmiyorsa ProductId'den otomatik çözümle
            if (req.ProductSellerItemId <= 0 && req.ProductId.HasValue && req.ProductId.Value > 0)
            {
                var resolvedSellerItem = await _context.GetRepository<SellerItem>().GetAll(false).AsNoTracking()
                    .Where(x => x.ProductId == req.ProductId.Value && x.Status == 1 && x.Stock > 0)
                    .OrderByDescending(x => x.Stock)
                    .FirstOrDefaultAsync();
                if (resolvedSellerItem != null)
                    req.ProductSellerItemId = resolvedSellerItem.Id;
                else
                {
                    var anySellerItem = await _context.GetRepository<SellerItem>().GetAll(false).AsNoTracking()
                        .Where(x => x.ProductId == req.ProductId.Value && x.Status == 1)
                        .FirstOrDefaultAsync();
                    if (anySellerItem != null)
                        req.ProductSellerItemId = anySellerItem.Id;
                    else
                    {
                        rs.AddError("Bu ürün için aktif ilan bulunamadı.");
                        return rs;
                    }
                }
            }
            var sem = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try{
                var sellerItem = await _context.GetRepository<SellerItem>().GetAll(false).AsNoTracking().Include(x => x.Seller).Include(x => x.Product).AsSingleQuery().Where(x => x.Id == req.ProductSellerItemId && x.Status != (int) EntityStatus.Deleted).FirstOrDefaultAsync();
                if(sellerItem == null){
                    rs.AddError("Ürün bulunamadı.");
                    return rs;
                }
                if(sellerItem.Status == 0){
                    rs.AddError("Ürünün ilan durumu pasif durumdadır.");
                    return rs;
                }
                var cartRepo = _context.GetRepository<CartItem>();
                var cartItem = await cartRepo.GetAll(false).AsNoTracking().AsSingleQuery().FirstOrDefaultAsync(x => x.ProductSellerItemId == req.ProductSellerItemId && x.UserId == userId);
                var requestedQuantity = req.Quantity; // positive for add, negative for remove
                if(cartItem == null){
                    if(requestedQuantity <= 0){
                        return await GetCart();
                    }
                    var initialQty = Math.Min(requestedQuantity, sellerItem.Stock);
                    cartItem = new CartItem{
                        CreatedDate = DateTime.Now,
                        CreatedId = userId,
                        UserId = userId,
                        ProductId = sellerItem.ProductId,
                        ProductSellerItemId = sellerItem.Id,
                        Quantity = Convert.ToInt32(initialQty),
                        Status = (int) EntityStatus.Active,
                        Voucher = sellerItem.Product?.IsPackageProduct == true ? req.Voucher : null,
                        GuideName = sellerItem.Product?.IsPackageProduct == true ? req.GuideName : null,
                        VisitDate = sellerItem.Product?.IsPackageProduct == true ? req.VisitDate : null,
                        PackageItemQuantitiesJson = sellerItem.Product?.IsPackageProduct == true && req.PackageItemQuantities != null && req.PackageItemQuantities.Count > 0
                            ? System.Text.Json.JsonSerializer.Serialize(req.PackageItemQuantities) : null
                    };
                    await cartRepo.InsertAsync(cartItem);
                    await _context.SaveChangesAsync();
                    rs.AddSuccess($"{sellerItem.Product.Name} isimli ürün sepete eklemiştir.");
                    var newCartResult = await GetCart();
                    newCartResult.Metadata = rs.Metadata; // Preserve the success message
                    return newCartResult;
                }
                var newQuantityCandidate = requestedQuantity != 0 ? cartItem.Quantity + requestedQuantity : cartItem.Quantity;
                if(newQuantityCandidate > sellerItem.Stock){
                    rs.AddError($"Bu üründe yeterli stok bulunmamaktadır. Stok: {sellerItem.Stock}");
                    return rs;
                }
                cartItem ??= new CartItem{
                    CreatedDate = DateTime.Now,
                    CreatedId = userId,
                    UserId = userId,
                    ProductId = sellerItem.ProductId,
                    ProductSellerItemId = sellerItem.Id
                };
                var entityToUpdate = cartItem.Id > 0 ? await cartRepo.FindAsync(cartItem.Id) : cartItem;
                entityToUpdate.Status = (int) EntityStatus.Active;
                entityToUpdate.Quantity = newQuantityCandidate;
                if (sellerItem.Product?.IsPackageProduct == true)
                {
                    if (!string.IsNullOrWhiteSpace(req.Voucher)) entityToUpdate.Voucher = req.Voucher;
                    if (!string.IsNullOrWhiteSpace(req.GuideName)) entityToUpdate.GuideName = req.GuideName;
                    if (req.VisitDate.HasValue) entityToUpdate.VisitDate = req.VisitDate;
                    if (req.PackageItemQuantities != null && req.PackageItemQuantities.Count > 0)
                        entityToUpdate.PackageItemQuantitiesJson = System.Text.Json.JsonSerializer.Serialize(req.PackageItemQuantities);
                }
                if(entityToUpdate.Quantity < 1 && entityToUpdate.Id > 0){
                    if(_context.DbContext.Entry(entityToUpdate).State == EntityState.Detached){
                        _context.DbContext.Attach(entityToUpdate);
                        rs.AddSuccess("ok");
                    }
                    cartRepo.Delete(entityToUpdate);
                    rs.AddSuccess("Ürün sepetten silindi.");
                } else
                    if(entityToUpdate.Id > 0){
                        entityToUpdate.ModifiedId = userId;
                        entityToUpdate.ModifiedDate = DateTime.Now;
                        entityToUpdate.Status = (int) EntityStatus.Active;
                    }
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(!lastResult.IsOk){
                    rs.AddError(lastResult.Exception.ToString());
                    return rs;
                } else{
                    rs.AddSuccess("ürün Sepete Eklendi");
                }
                var updatedCartResult = await GetCart();
                updatedCartResult.Metadata = rs.Metadata; // Preserve the success message
                return updatedCartResult;
            } finally{
                sem.Release();
            }
        } catch(Exception e){
            rs.AddError(e.Message);
            Console.WriteLine(e);
            throw;
        }
    }
    public async Task<IActionResult<CartDto>> GetCart(CartCustomerSavedPreferences? preferences = null){
        var response = OperationResult.CreateResult<CartDto>();
        try{
            var principal = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var _)){
                response.Result = new CartDto();
                return response;
            }
            // Ensure OrderManager sees the same principal
            _currentUser.SetUser(principal!);
            var execStrategy = _context.DbContext.Database.CreateExecutionStrategy();
            var cartItems = await execStrategy.ExecuteAsync(async () => await _orderManager.GetShoppingCart());
            CartCustomerSavedPreferences ? cartPreferences = preferences;
            if (cartPreferences == null)
            {
                try{
                    var cookies = _httpContextAccessor.HttpContext?.Request?.Cookies;
                    var prefRaw = cookies != null && cookies.ContainsKey(CartConsts.CartPreferencesStorageKey) ? cookies[CartConsts.CartPreferencesStorageKey] : string.Empty;
                    cartPreferences = JsonConvert.DeserializeObject<CartCustomerSavedPreferences>(prefRaw ?? string.Empty);
                } catch{
                    // ignored
                }
            }
            var cartResult = await execStrategy.ExecuteAsync(async () => await _orderManager.CalculateShoppingCart(cartItems, cartPreferences));
            response.Result = _mapper.Map<CartDto>(cartResult);
        } catch(Exception ex){
            response.AddSystemError(ex.ToString());
        }
        return response;
    }
    public async Task<IActionResult<CartDto>> CartItemRemove(int Id){
        var response = OperationResult.CreateResult<CartDto>();
        try{
            var principal = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId)){
                response.Result = new CartDto();
                return response;
            }
            var cartRepo = _context.GetRepository<CartItem>();
            await cartRepo.GetAll(true).Where(x => x.UserId == userId && x.Id == Id).ExecuteDeleteAsync();
            response.AddSuccess("Ürün sepetten kaldırıldı.");
            var removeCartResult = await GetCart();
            removeCartResult.Metadata = response.Metadata; // Preserve the success message
            return removeCartResult;
        } catch(Exception ex){
            var result = OperationResult.CreateResult<CartDto>();
            result.AddSystemError(ex.ToString());
            return result;
        }
    }
    public async Task<IActionResult<CartDto>> ClearCart(){
        var response = OperationResult.CreateResult<CartDto>();
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId)){
            response.Result = new CartDto();
            return response;
        }
        try{
            var cartRepo = _context.GetRepository<CartItem>();
            await cartRepo.GetAll(true).Where(x => x.UserId == userId).ExecuteDeleteAsync();
            response.AddSuccess("Sepet temizlendi.");
            var clearCartResult = await GetCart();
            clearCartResult.Metadata = response.Metadata; // Preserve the success message
            return clearCartResult;
        } catch(Exception ex){
            var result = OperationResult.CreateResult<CartDto>();
            result.AddSystemError(ex.ToString());
            return result;
        }
    }
    public async Task<IActionResult<CartDto>> PassiveCartItemBySellerId(int sellerId, bool status){
        var response = OperationResult.CreateResult<CartDto>();
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId)){
            response.Result = new CartDto();
            return response;
        }
        try{
            await _context.DbContext.CartItems
                .Include(x => x.ProductSellerItem)
                .Where(x => x.ProductSellerItem.SellerId == sellerId && x.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, status ? (int) EntityStatus.Active : (int) EntityStatus.Passive)
                    .SetProperty(a => a.ModifiedDate, DateTime.Now)
                    .SetProperty(a => a.ModifiedId, userId));
            
            response.AddSuccess(status ? "Satıcı ürünleri aktif edildi." : "Satıcı ürünleri pasif edildi.");
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
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId)){
            response.Result = new CartDto();
            return response;
        }
        try{
            await _context.DbContext.CartItems
                .Where(x => x.ProductSellerItemId == productSellerItemId && x.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, status ? (int) EntityStatus.Active : (int) EntityStatus.Passive)
                    .SetProperty(a => a.ModifiedDate, DateTime.Now)
                    .SetProperty(a => a.ModifiedId, userId));
            
            response.AddSuccess(status ? "Ürün aktif edildi." : "Ürün pasif edildi.");
            var updatedCart = await GetCart();
            updatedCart.Metadata = response.Metadata;
            return updatedCart;
        } catch(Exception e){
            response.AddSystemError(e.ToString());
            return response;
        }
    }
}
