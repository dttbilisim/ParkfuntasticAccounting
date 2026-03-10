using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Rules.Providers;
using ecommerce.EFCore.Context;

using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NRules;
namespace ecommerce.Domain.Shared.Services;
public class OrderManager : IOrderManager{
    protected IUnitOfWork<ApplicationDbContext> Context{get;}
    protected IRepository<CartItem> CartItemRepository{get;}
    protected IRepository<Orders> OrderRepository{get;}
    protected IRepository<Product> ProductRepository{get;}
    protected IRepository<Discount> DiscountRepository{get;}
    protected IRuleEngineRepository RuleEngineRepository{get;}

    protected CurrentUser CurrentUser{get;}
    protected IMemoryCache Cache{get;}
    protected IServiceScopeFactory ScopeFactory{get;}
    
    public OrderManager(IUnitOfWork<ApplicationDbContext> context, IRuleEngineRepository ruleEngineRepository, IMemoryCache cache, CurrentUser currentUser, IServiceScopeFactory scopeFactory){
        Context = context;
        RuleEngineRepository = ruleEngineRepository;
        Cache = cache;
        CurrentUser = currentUser;
        ScopeFactory = scopeFactory;
        CartItemRepository = context.GetRepository<CartItem>();
        OrderRepository = context.GetRepository<Orders>();
        ProductRepository = context.GetRepository<Product>();
        DiscountRepository = context.GetRepository<Discount>();
    }
    public async Task<List<CartItem>> GetShoppingCart(){
        var userId = CurrentUser.GetUserId();
        var cart = await CartItemRepository.GetAll(predicate: i => i.UserId == userId && i.Status == (int)EntityStatus.Active, disableTracking: true)
            .AsNoTracking()
            .Include(i => i.Product)
                .ThenInclude(p => p.Categories)
            .Include(i => i.Product)
                .ThenInclude(p => p.ProductImage)
            .Include(i => i.Product)
                .ThenInclude(p => p.ProductTiers)
            .Include(i => i.Product)
                .ThenInclude(p => p.ProductSaleItemsAsRef)
                    .ThenInclude(ps => ps.Product)
            .Include(i => i.ProductSellerItem)
                .ThenInclude(psi => psi.Seller)
                    .ThenInclude(s => s.CompanyCargos.Where(c => c.Status == (int)EntityStatus.Active))
                        .ThenInclude(cc => cc.Cargo)
                            .ThenInclude(c => c.CargoProperties.Where(p => p.Status == (int)EntityStatus.Active))
            .ToListAsync(); 
        return cart;
    }
    public async Task ClearShoppingCart(int companyId, int ? sellerId = null){
        if(sellerId.HasValue){
            var cartItems = await CartItemRepository.GetAll(predicate:null, disableTracking:false).Include(c => c.ProductSellerItem).Where(x => x.ProductSellerItem.SellerId == sellerId && x.UserId == companyId && x.Status == (int) EntityStatus.Active).ToListAsync();
            CartItemRepository.Delete(cartItems);
            await Context.SaveChangesAsync();
        } else{
            await CartItemRepository.GetAll(predicate:null, disableTracking:false).Where(x => x.UserId == companyId && x.Status == (int) EntityStatus.Active).ExecuteDeleteAsync();
        }
    }
    public async Task<CartResult> CalculateShoppingCart(List<CartItem> cart, CartCustomerSavedPreferences ? cartPreferences = null, bool includeCommissions = false){
        CartCargoResult MapCompanyCargo(CompanyCargo companyCargo)
        {
            var properties = companyCargo.Cargo?.CargoProperties?
                .Where(p => p.Status == (int)EntityStatus.Active)
                .OrderBy(p => p.DesiMinValue)
                .ThenBy(p => p.DesiMaxValue)
                .Select(p => new CartCargoPropertyResult
                {
                    Size = p.Size,
                    DesiMinValue = p.DesiMinValue,
                    DesiMaxValue = p.DesiMaxValue,
                    Price = p.Price
                })
                .ToList() ?? new List<CartCargoPropertyResult>();

            return new CartCargoResult
            {
                CargoId = companyCargo.CargoId,
                CompanyCargoId = companyCargo.Id,
                Name = companyCargo.Cargo?.Name ?? string.Empty,
                Message = companyCargo.Cargo?.Message,
                CargoPrice = companyCargo.Cargo?.Amount ?? decimal.Zero,
                CargoOverloadPrice = companyCargo.Cargo?.CargoOverloadPrice ?? decimal.Zero,
                MinBasketAmount = companyCargo.MinBasketAmount,
                IsDefault = companyCargo.IsDefault,
                IsLocalStorage = companyCargo.Cargo?.IsLocalStorage ?? false,
                Properties = properties,
                CargoType = companyCargo.Cargo?.CargoType ?? ecommerce.Core.Utils.CargoType.Standard,
                CoveredKm = companyCargo.Cargo?.CoveredKm ?? decimal.Zero,
                PricePerExtraKm = companyCargo.Cargo?.PricePerExtraKm ?? decimal.Zero,
                BaseFeeAmount = companyCargo.Cargo?.Amount ?? decimal.Zero
            };
        }

        decimal CalculateItemDesi(CartItem item)
        {
            var width = item.Product?.Width ?? 0m;
            var height = item.Product?.Height ?? 0m;
            var length = item.Product?.Length ?? 0m;

            if (width > 0 && height > 0 && length > 0)
            {
                var desi = (width * height * length) / 3000m;
                return desi > 0 ? desi : 1m;
            }

            if (item.Quantity > 0 && item.ProductDesi > 0)
            {
                var perItem = item.ProductDesi / item.Quantity;
                return perItem > 0 ? perItem : 1m;
            }

            return 1m;
        }

        void ApplyCargoPricing(CartSellerResult seller, List<CompanyCargo> companyCargos)
        {
            if (seller.Cargoes == null || !seller.Cargoes.Any())
            {
                return;
            }

            var activeItems = seller.Items
                .Where(i => i.Status == (int)EntityStatus.Active && !i.IsGiftProduct)
                .ToList();

            foreach (var cargo in seller.Cargoes)
            {
                var companyCargo = companyCargos.FirstOrDefault(cc => cc.Id == cargo.CompanyCargoId);
                if (companyCargo == null)
                {
                    continue;
                }

                var cargoEntity = companyCargo.Cargo;
                var cargoType = cargoEntity?.CargoType ?? ecommerce.Core.Utils.CargoType.Standard;

                if (cargoType == ecommerce.Core.Utils.CargoType.BicopsExpress)
                {
                    // Mesafe bazlı hesaplama: tek ücret (Amount) + kapsanan km (CoveredKm) + sonrası km başı (PricePerExtraKm)
                    var baseFee = cargoEntity?.Amount ?? decimal.Zero;
                    var coveredKm = cargoEntity?.CoveredKm ?? decimal.Zero;
                    var pricePerExtraKm = cargoEntity?.PricePerExtraKm ?? decimal.Zero;

                    double? distanceKm = null;
                    if (cartPreferences?.DistanceKmBySellerId != null && cartPreferences.DistanceKmBySellerId.TryGetValue(seller.SellerId, out var d))
                    {
                        distanceKm = d;
                    }

                    decimal totalPrice;
                    if (distanceKm.HasValue && distanceKm.Value >= 0)
                    {
                        var dist = (decimal)distanceKm.Value;
                        if (dist <= coveredKm)
                        {
                            totalPrice = baseFee;
                        }
                        else
                        {
                            var extraKm = dist - coveredKm;
                            totalPrice = baseFee + Math.Round(extraKm * pricePerExtraKm, 2, MidpointRounding.AwayFromZero);
                        }
                    }
                    else
                    {
                        totalPrice = baseFee;
                    }

                    if (companyCargo.MinBasketAmount > 0 && seller.SubTotal >= companyCargo.MinBasketAmount)
                    {
                        totalPrice = 0m;
                    }

                    cargo.SelectedProperty = null;
                    cargo.CargoPrice = totalPrice;
                    continue;
                }

                var properties = companyCargo.Cargo?.CargoProperties?
                    .Where(p => p.Status == (int)EntityStatus.Active)
                    .OrderBy(p => p.DesiMinValue)
                    .ToList() ?? new List<CargoProperty>();

                decimal totalPriceDesi = 0m;

                foreach (var item in activeItems)
                {
                    var itemDesi = CalculateItemDesi(item);
                    var property = properties.FirstOrDefault(p => itemDesi >= p.DesiMinValue && itemDesi <= p.DesiMaxValue);

                    if (property != null)
                    {
                        totalPriceDesi += property.Price * item.Quantity;
                    }
                    else if (properties.Any())
                    {
                        var maxProperty = properties.OrderBy(p => p.DesiMaxValue).Last();
                        var overloadPrice = companyCargo.Cargo?.CargoOverloadPrice ?? decimal.Zero;
                        totalPriceDesi += (maxProperty.Price + overloadPrice) * item.Quantity;
                    }
                    else
                    {
                        totalPriceDesi += (companyCargo.Cargo?.Amount ?? 0m) * item.Quantity;
                    }
                }

                if (companyCargo.MinBasketAmount > 0 && seller.SubTotal >= companyCargo.MinBasketAmount)
                {
                    totalPriceDesi = 0m;
                }

                cargo.SelectedProperty = null;
                cargo.CargoPrice = totalPriceDesi;
            }
        }

        var result = new CartResult();
        // Optimize: Load discounts and company rates in parallel using separate DbContext scopes
        // Each method uses its own scope to avoid "command already in progress" errors
        var discountsTask = GetAllAllowedDiscountsAsync(cart, cartPreferences?.UsedCouponCode);
        var companyRatesTask = includeCommissions ? GetAllCompanyRatesAsync(cart) : Task.FromResult<List<CompanyRate>?>(null);
        
        // Wait for both tasks in parallel (each uses its own DbContext)
        await Task.WhenAll(discountsTask, companyRatesTask);
        
        var discounts = await discountsTask;
        var companyRates = await companyRatesTask;
        
        var cartAppliedDiscounts = discounts.Where(c => c.DiscountType == DiscountType.AssignedToCart).ToList();
        var cargoAppliedDiscounts = discounts.Where(c => c.DiscountType == DiscountType.AssignedToCargo).ToList();
        var sellerGroups = cart.GroupBy(c => c.ProductSellerItem.SellerId).ToList();
        var sellerIds = sellerGroups.Select(g => g.Key).ToList();

        var companyCargoRepository = Context.GetRepository<CompanyCargo>();
        // Tüm aktif CompanyCargo kayıtları (Standard + BicopsExpress / Hızlı Kurye) — CargoType filtresi yok; satıcıya atanmış her kargo sepette listelenir
        var sellerCargoRecords = await companyCargoRepository
            .GetAll(predicate: null, disableTracking: true)
            .Where(cc => sellerIds.Contains(cc.SellerId) && cc.Status == (int)EntityStatus.Active)
            .Include(cc => cc.Cargo)
                .ThenInclude(c => c.CargoProperties.Where(p => p.Status == (int)EntityStatus.Active))
            .ToListAsync();

        var sellerCargoLookup = sellerCargoRecords
            .GroupBy(cc => cc.SellerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sellerGiftProducts = new List<(CartItem cartItem, List<Discount> discounts)>();
        foreach(var sellerGroup in sellerGroups){
            var sellerEntity = sellerGroup.First().ProductSellerItem.Seller;
            var sellerCargoes = sellerCargoLookup.TryGetValue(sellerEntity.Id, out var cargoList)
                ? cargoList
                : new List<CompanyCargo>();
            var seller = new CartSellerResult{
                SellerId = sellerEntity.Id,
                SellerName = sellerEntity.Name!,
                SellerPoint = 0,
                MinCartTotal = 0,
                IyzicoSubmerhantKey = "",
                IsAllItemsPassive = true,
                SellerTaxNumber = "",
                SellerAddress = sellerEntity.Address,
                SellerPhoneNumber = sellerEntity.PhoneNumber,
                SellerTaxName = "",
                SellerEmail = sellerEntity.Email,
                IsBlueTick =false,
                Cargoes = sellerCargoes.Select(MapCompanyCargo).ToList()
                
                    
            };
            result.Sellers.Add(seller);
            foreach(var cartItem in sellerGroup){
                if (cartItem.Quantity <= 0) 
                {
                    cartItem.Quantity = 1;
                }
                
                seller.Items.Add(cartItem);
                // Backend'de image URL hesaplama
                cartItem.PictureUrl = cartItem.Product?.ProductImage?.FirstOrDefault()?.FileGuid ?? cartItem.Product?.DocumentUrl;
                
                if(cartItem.Product is not{Status: (int) EntityStatus.Active} || cartItem.ProductSellerItem is not{Status: (int) EntityStatus.Active}){
                    cartItem.Warnings.Add("Ürün şu anda satışta değil.");
                    cartItem.Status = (int) EntityStatus.Passive;
                }
                
                if(cartItem.ProductSellerItem.Stock < cartItem.Quantity){
                    cartItem.Warnings.Add("Yeterli stok bulunmamaktadır.");
                }
                // if(cartItem.ProductSellerItem.Stock > 0 && cartItem.ProductSellerItem.Stock < cartItem.Quantity){
                //     cartItem.Warnings.Add($"Maksimum {cartItem.ProductSellerItem.Stock} adet satın alabilirsiniz.");
                // }
                // if(cartItem.ProductSellerItem.Stock > 0 && cartItem.ProductSellerItem.Stock > cartItem.Quantity){
                //     cartItem.Warnings.Add($"Minimum {cartItem.ProductSellerItem.Stock} adet satın alabilirsiniz.");
                // }
                // Paket ürünlerde fiyat = içeriklerdeki (ProductSaleItems) fiyat * miktar toplamı
                var productPriceWithoutDiscount = cartItem.Product?.IsPackageProduct == true
                    && cartItem.Product?.ProductSaleItemsAsRef != null
                    && cartItem.Product.ProductSaleItemsAsRef.Any()
                    ? cartItem.Product.ProductSaleItemsAsRef.Sum(ps => ps.Price * (cartItem.PackageItemQuantities?.GetValueOrDefault(ps.ProductId) ?? 1))
                    : cartItem.ProductSellerItem.SalePrice;
                var productAppliedDiscounts = discounts.Where(c => !c.HasGiftProducts && (c.AssignedSellerItemIds?.Contains(cartItem.ProductSellerItemId) ?? false)).ToList();
                productAppliedDiscounts = GetPreferredDiscount(productAppliedDiscounts, productPriceWithoutDiscount, out var appliedDiscountAmount);
                cartItem.AppliedDiscounts = productAppliedDiscounts;
                cartItem.DiscountAmount = appliedDiscountAmount;
                cartItem.UnitPriceWithoutDiscount = productPriceWithoutDiscount;
                cartItem.UnitPrice = productPriceWithoutDiscount - appliedDiscountAmount;
                if(cartItem.UnitPrice < decimal.Zero){
                    cartItem.UnitPrice = decimal.Zero;
                }
                if(cartItem.UnitPriceWithoutDiscount < decimal.Zero){
                    cartItem.UnitPriceWithoutDiscount = decimal.Zero;
                }
                cartItem.TotalWithoutDiscount = cartItem.UnitPriceWithoutDiscount * cartItem.Quantity;
                cartItem.Total = cartItem.UnitPrice * cartItem.Quantity;
                // UI'da gösterilecek kazanç: gösterilen fiyata göre yüzde ise yeniden hesapla
                if (productAppliedDiscounts.Any() && productAppliedDiscounts.All(d => d.UsePercentage))
                {
                    var pct = productAppliedDiscounts.Sum(d => d.DiscountPercentage ?? 0m);
                    // Görünen fiyata göre % tasarruf
                    cartItem.DisplaySavings = Math.Round(cartItem.UnitPrice * pct / 100m, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    cartItem.DisplaySavings = cartItem.DiscountAmount;
                }
                if(cartItem.Status != (int) EntityStatus.Active){
                    continue;
                }
                if(companyRates != null){
                    // Hierarchy: 1. SellerItem.Commision (product-specific), 2. CompanyRate (tier/category)
                    var sellerItemCommission = cartItem.ProductSellerItem?.Commision ?? 0;
                    
                    if(sellerItemCommission > 0){
                        // Use product-specific commission from SellerItem
                        cartItem.CommissionRateId = null; // Not from CompanyRate table
                        cartItem.CommissionRatePercent = sellerItemCommission;
                        cartItem.CommissionTotal = cartItem.Total * sellerItemCommission / 100;
                    }
                    else{
                        // Fallback to CompanyRate (existing logic)
                        var productCommissionRate = companyRates.Where(c => cartItem.Product.ProductTiers.Any(t => t.TierId == c.TierId)).MaxBy(c => c.Rate) ?? companyRates.Where(c => c.ProductId == cartItem.ProductId).MaxBy(c => c.Rate) ?? companyRates.Where(c => cartItem.Product.Categories.Any(t => t.CategoryId == c.CategoryId)).MaxBy(c => c.Rate);
                        cartItem.CommissionRateId = productCommissionRate?.Id;
                        cartItem.CommissionRatePercent = productCommissionRate?.Rate ?? 0;
                        cartItem.CommissionTotal = productCommissionRate?.Rate > 0 ? cartItem.Total * productCommissionRate.Rate / 100 : 0;
                    }
                }
                // if(includeCommissions && company.Rate > 0){
                //     cartItem.SubmerhantCommisionRate = company.Rate.Value;
                //     cartItem.SubmerhantCommision = cartItem.Total * company.Rate.Value / 100;
                // }
                var productDimension = cartItem.Product.Width * cartItem.Product.Height * cartItem.Product.Length;
                cartItem.ProductDesi = (productDimension > 0 ? productDimension / 3000 : 0) * cartItem.Quantity;
                seller.Desi += cartItem.ProductDesi;
                seller.SubTotal += cartItem.TotalWithoutDiscount;
                seller.OrderTotal += cartItem.Total;
                seller.OrderTotalDiscount += cartItem.DiscountAmount * cartItem.Quantity;
                seller.TotalItems += cartItem.Quantity;
                seller.IsAllItemsPassive = false;
              
                var discountsForGift = discounts.Where(c => c.HasGiftProducts && (c.AssignedSellerItemIds?.Contains(cartItem.ProductSellerItemId) ?? false)).ToList();
                sellerGiftProducts.Add((cartItem, discountsForGift));
            }
            var sellerCartAppliedDiscounts = GetPreferredDiscount(cartAppliedDiscounts, seller.OrderTotal, out var cartAppliedDiscountAmount);
            if(seller.OrderTotal < cartAppliedDiscountAmount){
                cartAppliedDiscountAmount = seller.OrderTotal;
            }
            seller.OrderTotal -= cartAppliedDiscountAmount;
            if(seller.OrderTotal < decimal.Zero){
                seller.OrderTotal = decimal.Zero;
            }
            seller.OrderTotalDiscount += cartAppliedDiscountAmount;
            seller.AppliedDiscounts = sellerCartAppliedDiscounts;
            seller.Desi = seller.Desi > 0 ? seller.Desi : 1;
            ApplyCargoPricing(seller, sellerCargoes);
            
            // Kargo seçimi kaldırıldı - normal ürüne geçildi, kargo ücreti 0
            seller.SelectedCargo = null;
            seller.CargoPrice = decimal.Zero;
            var sellerCargoAppliedDiscounts = GetPreferredDiscount(new List<Discount>(), seller.CargoPrice, out var cargoAppliedDiscountAmount);
            if(seller.CargoPrice > decimal.Zero && sellerCargoAppliedDiscounts.Any()){
                if(seller.CargoPrice < cargoAppliedDiscountAmount){
                    cargoAppliedDiscountAmount = seller.CargoPrice;
                }
                seller.CargoPrice -= cargoAppliedDiscountAmount;
                if(seller.CargoPrice < decimal.Zero){
                    seller.CargoPrice = decimal.Zero;
                }
                seller.CargoDiscount = cargoAppliedDiscountAmount;
                seller.AppliedDiscounts.AddRange(sellerCargoAppliedDiscounts);
            }
            if(!seller.Cargoes.Any()){
                seller.Warnings.Add("Satıcının kargo bilgisi bulunmamaktadır.");
            }
            // Passive seller'lar da görünsün (checkbox için gerekli)
            // if(seller.IsAllItemsPassive){
            //     continue;
            // }
            
            // Sadece active item'lar varsa kargo ekle
            if(!seller.IsAllItemsPassive){
                seller.OrderTotal += seller.CargoPrice;
            }
            if(seller.MinCartTotal > 0 && seller.OrderTotal < seller.MinCartTotal){
                // seller.Warnings.Add($"Satıcının minimum sepet tutarı {seller.MinCartTotal:N2} TL'dir.");
                result.SubTotal += seller.SubTotal;
                result.OrderTotal += seller.OrderTotal;
                result.OrderTotalDiscount += seller.OrderTotalDiscount;
                result.CargoPrice += seller.CargoPrice;
                result.CargoDiscount += seller.CargoDiscount;
                result.TotalItems += seller.TotalItems;
                result.CartCount = cart.Count(x => x.Status == 1);
                // sepette 2 kullanicidan ayni urun olunca hesaplamiyordu.
                continue;
            }
            result.SubTotal += seller.SubTotal;
            result.OrderTotal += seller.OrderTotal;
            result.OrderTotalDiscount += seller.OrderTotalDiscount;
            result.CargoPrice += seller.CargoPrice;
            result.CargoDiscount += seller.CargoDiscount;
            result.TotalItems += seller.TotalItems;
            result.CartCount = cart.Count(x => x.Status == 1);
        }
        if(sellerGiftProducts.Any()){
            var giftProductIds = sellerGiftProducts.SelectMany(c => c.discounts.SelectMany(d => d.GiftProductIds!)).Distinct().ToList();
            var giftSellerIds = sellerGiftProducts.SelectMany(c => new List<int>{c.cartItem.ProductSellerItem.SellerId}.Concat(c.discounts.Where(d => d.GiftProductSellerId.HasValue).Select(d => d.GiftProductSellerId!.Value))).Distinct().ToList();
            
            // Optimize: Only query if we have gift products
            List<SellerItem> allGiftProducts = new List<SellerItem>();
            if(giftProductIds.Any()){
                var missingSellerCargoIds = giftSellerIds.Where(id => !sellerCargoLookup.ContainsKey(id)).ToList();
                if(missingSellerCargoIds.Any()){
                    var additionalCargos = await companyCargoRepository
                        .GetAll(predicate: null, disableTracking: true)
                        .Where(cc => missingSellerCargoIds.Contains(cc.SellerId) && cc.Status == (int)EntityStatus.Active)
                        .Include(cc => cc.Cargo)
                            .ThenInclude(c => c.CargoProperties.Where(p => p.Status == (int)EntityStatus.Active))
                        .ToListAsync();

                    foreach(var group in additionalCargos.GroupBy(cc => cc.SellerId)){
                        sellerCargoLookup[group.Key] = group.ToList();
                    }
                }
                allGiftProducts = await Context.GetRepository<SellerItem>().
                    GetAll(predicate:null, disableTracking:true).Include(p => p.Seller)
                    .ThenInclude(s => s.CompanyCargos.Where(c => c.Status == (int) EntityStatus.Active))
                    .ThenInclude(i => i.Cargo).ThenInclude(i => i.CargoProperties
                        .Where(c => c.Status == (int) EntityStatus.Active)).Include(p => p.Product.ProductImage)
                    .Where(p => giftProductIds.Contains(p.ProductId) && p.Status == (int) EntityStatus.Active && giftSellerIds.Contains(p.SellerId)).ToListAsync();
            }
            
            foreach(var (cartItem, discountsForGift) in sellerGiftProducts){
                if(cartItem.Status != (int) EntityStatus.Active){
                    continue;
                }
                var giftQuantity = cartItem.Quantity;
                foreach(var giftDiscount in discountsForGift){
                    if(giftDiscount.UseSingleGiftProduct){
                        giftQuantity = 1;
                    }
                    var giftSeller = allGiftProducts.FirstOrDefault(p => giftDiscount.GiftProductSellerId.HasValue ? p.SellerId == giftDiscount.GiftProductSellerId.Value : p.SellerId == cartItem.ProductSellerItem.SellerId)?.Seller;
                    if(giftSeller == null){
                        continue;
                    }
                    var giftSellerCargoes = sellerCargoLookup.TryGetValue(giftSeller.Id, out var giftCargoList)
                        ? giftCargoList
                        : new List<CompanyCargo>();
                    var giftSellerResult = result.Sellers.FirstOrDefault(s => s.SellerId == giftSeller.Id);
                    var isNewGiftSeller = false;
                    if(giftSellerResult == null){
                        giftSellerResult = new CartSellerResult{
                            SellerId = giftSeller.Id,
                            SellerName = giftSeller.Name ?? string.Empty,
                            SellerPoint = 0,
                            MinCartTotal = 0,
                            IsAllItemsPassive = true,
                            Cargoes = giftSellerCargoes.Select(MapCompanyCargo).ToList()
                        };
                        isNewGiftSeller = true;
                    }
                    var giftProducts = allGiftProducts.Where(p => giftDiscount.GiftProductIds!.Contains(p.ProductId) && p.SellerId == giftSellerResult.SellerId).ToList();
                    foreach(var giftProduct in giftProducts){
                        var currentGiftProduct = giftSellerResult.Items.FirstOrDefault(i => i.ProductSellerItemId == giftProduct.Id && i.IsGiftProduct);
                        if(currentGiftProduct != null){
                            if(currentGiftProduct.Quantity >= giftQuantity){
                                continue;
                            }
                            giftSellerResult.Items.Remove(currentGiftProduct);
                        }
                       
                        if(giftProduct.Stock < giftQuantity){
                            continue;
                        }
                        var isPassivedByUser = cartPreferences?.IgnoredGiftProducts.Contains(giftProduct.Id) ?? false;
                        var giftCartItem = new CartItem{
                            UserId = cartItem.UserId,
                            ProductId = giftProduct.ProductId,
                            ProductSellerItemId = giftProduct.Id,
                            Quantity = giftQuantity,
                            Status = isPassivedByUser ? (int) EntityStatus.Passive : cartItem.Status,
                            CreatedDate = DateTime.Now,
                            Product = giftProduct.Product,
                            User = cartItem.User,
                            ProductSellerItem = null,
                            ProductDesi = 0,
                            IsReadonly = true,
                            IsGiftProduct = true
                        };
                        var giftProductPriceWithoutDiscount = giftProduct.SalePrice;
                        var giftProductAppliedDiscounts = new List<Discount>{giftDiscount};
                        giftProductAppliedDiscounts = GetPreferredDiscount(giftProductAppliedDiscounts, giftProductPriceWithoutDiscount, out var giftAppliedDiscountAmount);
                        giftCartItem.AppliedDiscounts = giftProductAppliedDiscounts;
                        giftCartItem.DiscountAmount = giftAppliedDiscountAmount;
                        giftCartItem.UnitPriceWithoutDiscount = giftProductPriceWithoutDiscount;
                        giftCartItem.UnitPrice = giftProductPriceWithoutDiscount - giftAppliedDiscountAmount;
                        if(giftCartItem.UnitPrice < decimal.Zero){
                            giftCartItem.UnitPrice = decimal.Zero;
                        }
                        if(giftCartItem.UnitPriceWithoutDiscount < decimal.Zero){
                            giftCartItem.UnitPriceWithoutDiscount = decimal.Zero;
                        }
                        giftCartItem.TotalWithoutDiscount = giftCartItem.UnitPriceWithoutDiscount * giftCartItem.Quantity;
                        giftCartItem.Total = giftCartItem.UnitPrice * giftCartItem.Quantity;
                        giftCartItem.CanGiftRemove = giftCartItem.Total > decimal.Zero;
                        if(giftCartItem.Status == (int) EntityStatus.Active){
                            giftSellerResult.SubTotal += giftCartItem.TotalWithoutDiscount;
                            giftSellerResult.OrderTotal += giftCartItem.Total;
                            giftSellerResult.OrderTotalDiscount += giftCartItem.DiscountAmount * giftCartItem.Quantity;
                            giftSellerResult.TotalItems += giftCartItem.Quantity;
                            giftSellerResult.IsAllItemsPassive = false;
                            result.SubTotal += giftCartItem.TotalWithoutDiscount;
                            result.OrderTotal += giftCartItem.Total;
                            result.OrderTotalDiscount += giftCartItem.DiscountAmount * giftCartItem.Quantity;
                            result.TotalItems += giftCartItem.Quantity;
                        }
                        giftSellerResult.Items.Add(giftCartItem);
                    }
                    ApplyCargoPricing(giftSellerResult, giftSellerCargoes);
                    giftSellerResult.SelectedCargo = null;
                    giftSellerResult.CargoPrice = decimal.Zero;
                    if(isNewGiftSeller && giftSellerResult.Items.Any()){
                        result.Sellers.Add(giftSellerResult);
                    }
                    if(!giftSellerResult.Items.Any(i => i.Status == (int) EntityStatus.Active && !i.IsGiftProduct)){
                        giftSellerResult.MinCartTotal = 0;
                        giftSellerResult.CargoPrice = 0;
                        giftSellerResult.Desi = 0;
                        if(giftSellerResult.SelectedCargo != null){
                            giftSellerResult.SelectedCargo.CargoPrice = 0;
                        }
                        giftSellerResult.Cargoes.ForEach(c => {
                                c.SelectedProperty = null;
                                c.CargoPrice = 0;
                            }
                        );
                    }
                }
            }
        }
        result.AppliedDiscounts = result.Sellers.SelectMany(s => s.AppliedDiscounts.Concat(s.Items.SelectMany(i => i.AppliedDiscounts))).DistinctBy(d => d.Id).ToList();
        var appliedCoupon = result.AppliedDiscounts.FirstOrDefault(c => c.RequiresCouponCode);
        result.IsCouponCodeApplied = appliedCoupon != null;
        result.AppliedCouponCode = result.IsCouponCodeApplied ? cartPreferences?.UsedCouponCode : null;
        result.AppliedCompanyCouponCodeId = appliedCoupon?.CompanyCoupons.FirstOrDefault(c => c.CouponCode == cartPreferences?.UsedCouponCode)?.Id;

        // Build DiscountSummaries for UI (Name, Percentage, Amount)
        var discountTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var discountPercent = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in result.Sellers.SelectMany(s => s.Items))
        {
            if ((item.DisplaySavings > 0 || item.DiscountAmount > 0) && (item.AppliedDiscounts?.Any() ?? false))
            {
                var disc = item.AppliedDiscounts.First();
                var name = disc.Name;
                var pct = disc.UsePercentage ? (disc.DiscountPercentage ?? 0) : (decimal?)null;
                var perItem = item.DisplaySavings > 0 ? item.DisplaySavings : item.DiscountAmount;
                discountTotals[name] = discountTotals.GetValueOrDefault(name) + (perItem * item.Quantity);
                if (!discountPercent.ContainsKey(name)) discountPercent[name] = pct;
            }
        }

        // Only distribute non-item discounts (cart/cargo level)
        var orderLevelDiscounts = result.AppliedDiscounts
            .Where(d => d.DiscountType == DiscountType.AssignedToCart || d.DiscountType == DiscountType.AssignedToCargo)
            .ToList();
        var productLevelTotal = result.Sellers.SelectMany(s => s.Items).Sum(i => i.DiscountAmount * i.Quantity);
        var otherTotal = result.OrderTotalDiscount - productLevelTotal;
        if (otherTotal > 0)
        {
            var weightGroups = orderLevelDiscounts
                .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new {
                    Name = g.Key,
                    Weight = g.Sum(d => d.UsePercentage && d.DiscountPercentage.HasValue
                        ? (result.SubTotal * (d.DiscountPercentage.Value / 100m))
                        : (d.DiscountAmount ?? 0m)),
                    Percentage = g.First().UsePercentage ? g.First().DiscountPercentage : null
                }).ToList();

            if (!weightGroups.Any())
            {
                weightGroups.Add(new { Name = "Cart Discount", Weight = otherTotal, Percentage = (decimal?)null });
            }

            var totalWeight = weightGroups.Sum(w => w.Weight);
            foreach (var w in weightGroups)
            {
                var share = totalWeight > 0 ? otherTotal * (w.Weight / totalWeight) : otherTotal / weightGroups.Count;
                discountTotals[w.Name] = discountTotals.GetValueOrDefault(w.Name) + share;
                if (!discountPercent.ContainsKey(w.Name)) discountPercent[w.Name] = w.Percentage;
            }
        }

        result.DiscountSummaries = discountTotals
            .Select(kv => new DiscountSummary { Name = kv.Key, Percentage = discountPercent.GetValueOrDefault(kv.Key), Amount = kv.Value })
            .OrderByDescending(d => d.Amount)
            .ToList();
        var minCartTotal = await GetAppMinimumCartTotal();
        if(minCartTotal > 0 && result.OrderTotal < minCartTotal){
            result.Warnings.Add($"Minimum sepet tutarı {minCartTotal:N2} TL'dir.");
        }

        var discount = discounts.Select(x=>x.HasGiftProducts);
        
        
        return result;
    }
    public async Task<List<Discount>> GetAllAllowedDiscountsAsync(List<CartItem> cart, string ? couponCode = null){
        var allowedDiscounts = new List<Discount>();

        // Use separate DbContext scope for parallel execution
        using var scope = ScopeFactory.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        var scopedDiscountRepo = scopedContext.GetRepository<Discount>();
        var scopedAppliedDiscountRepo = scopedContext.GetRepository<OrderAppliedDiscount>();
        
        // var discountsCacheKey = $"Discounts:{CurrentUser.GetCompanyId()}:{couponCode}";
        //
        // var discounts = await Cache.GetOrCreateAsync(
        //     discountsCacheKey,
        //     async entry =>
        //     {
        //         entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        //     });
        var now = DateTime.Now;
        var currentUserId = CurrentUser.GetUserId();
        // Optimize: Use AsNoTracking for read-only query and split complex Where conditions
        var discountsQuery = scopedDiscountRepo.GetAll(predicate: null, disableTracking: true)
            .Include(d => d.CompanyCoupons.Where(c => c.CompanyId == currentUserId && !c.IsUsed && c.CouponCode == couponCode))
            .Where(c => c.Status == (int)EntityStatus.Active 
                && (!c.StartDate.HasValue || c.StartDate.Value <= now) 
                && (!c.EndDate.HasValue || c.EndDate.Value >= now));
        
        // Apply coupon code filter only if required
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            discountsQuery = discountsQuery.Where(c => !c.RequiresCouponCode 
                || c.CouponCode == couponCode 
                || c.CompanyCoupons.Any(cc => cc.CompanyId == currentUserId && !cc.IsUsed && cc.CouponCode == couponCode));
        }
        else
        {
            discountsQuery = discountsQuery.Where(c => !c.RequiresCouponCode);
        }
        
        var discounts = await discountsQuery.ToListAsync();
        var limitedDiscounts = discounts.Where(d => d.DiscountLimitation != DiscountLimitationType.Unlimited).ToList();
        Dictionary<int, int> globalCounts = new();
        Dictionary<int, int> userCounts = new();

        if (limitedDiscounts.Any())
        {

            // 1. Global Usage Counts (NTimesOnly)
            var globalLimitIds = limitedDiscounts
                .Where(d => d.DiscountLimitation == DiscountLimitationType.NTimesOnly)
                .Select(d => d.Id)
                .ToList();

            if (globalLimitIds.Any())
            {
                var counts = await scopedAppliedDiscountRepo.GetAll(predicate: null, disableTracking: true)
                    .Where(ad => globalLimitIds.Contains(ad.DiscountId))
                    .GroupBy(ad => ad.DiscountId)
                    .Select(g => new { DiscountId = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var c in counts)
                {
                    globalCounts[c.DiscountId] = c.Count;
                }
            }

            // 2. User Specific Usage Counts (NTimesPerCustomer)
            var userLimitIds = limitedDiscounts
                .Where(d => d.DiscountLimitation == DiscountLimitationType.NTimesPerCustomer)
                .Select(d => d.Id)
                .ToList();

            if (userLimitIds.Any())
            {
                var userId = CurrentUser.GetUserId();
                var counts = await scopedAppliedDiscountRepo.GetAll(predicate: null, disableTracking: true)
                    .Where(ad => userLimitIds.Contains(ad.DiscountId) && ad.Order.CompanyId == userId)
                    .GroupBy(ad => ad.DiscountId)
                    .Select(g => new { DiscountId = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var c in counts)
                {
                    userCounts[c.DiscountId] = c.Count;
                }
            }
        }

        // First pass: Filter discounts that don't need rule validation
        var discountsToValidate = new List<Discount>();
        foreach(var discount in discounts){
            if(discount.DiscountLimitation != DiscountLimitationType.Unlimited){
                var usedTimes = 0;
                switch(discount.DiscountLimitation){
                    case DiscountLimitationType.NTimesOnly:
                        usedTimes = globalCounts.GetValueOrDefault(discount.Id, 0);
                        break;
                    case DiscountLimitationType.NTimesPerCustomer:
                        usedTimes = userCounts.GetValueOrDefault(discount.Id, 0);
                        break;
                }
                if(usedTimes >= discount.LimitationTimes){
                    continue;
                }
            }
          
            if(!new[]{DiscountType.AssignedToCart,DiscountType.AssignedToCargo}.Contains(discount.DiscountType)){
                if(cart is not{Count: > 0}){
                    continue;
                }
                discount.AssignedSellerItemIds = new List<int>();
                var assignedEntityIds = discount.AssignedEntityIds ?? new List<int>();
                var assignedSellerIds = discount.AssignedSellerIds ?? new List<int>();
                switch(discount.DiscountType){
                   
                    case DiscountType.AssignedToProducts:
                        discount.AssignedSellerItemIds = assignedSellerIds.Count > 0
                            ? cart.Where(c => assignedEntityIds.Contains(c.ProductId) && assignedSellerIds.Contains(c.ProductSellerItem.SellerId))
                                  .Select(c => c.ProductSellerItemId).ToList()
                            : cart.Where(c => assignedEntityIds.Contains(c.ProductId))
                                  .Select(c => c.ProductSellerItemId).ToList();
                        break;
                    case DiscountType.AssignedToCategories:
                        discount.AssignedSellerItemIds = assignedSellerIds.Count > 0
                            ? cart.Where(c => c.Product?.Categories != null
                                               && assignedEntityIds.Any(a => c.Product.Categories.Any(pc => pc.CategoryId == a))
                                               && assignedSellerIds.Contains(c.ProductSellerItem.SellerId))
                                  .Select(c => c.ProductSellerItemId).ToList()
                            : cart.Where(c => c.Product?.Categories != null
                                               && assignedEntityIds.Any(a => c.Product.Categories.Any(pc => pc.CategoryId == a)))
                                  .Select(c => c.ProductSellerItemId).ToList();
                        break;
                    case DiscountType.AssignedToBrands:
                        discount.AssignedSellerItemIds = assignedSellerIds.Count > 0
                            ? cart.Where(c => c.Product != null
                                               && assignedEntityIds.Contains(c.Product.BrandId)
                                               && assignedSellerIds.Contains(c.ProductSellerItem.SellerId))
                                  .Select(c => c.ProductSellerItemId).ToList()
                            : cart.Where(c => c.Product != null && assignedEntityIds.Contains(c.Product.BrandId))
                                  .Select(c => c.ProductSellerItemId).ToList();
                        break;
                }
                if(!discount.AssignedSellerItemIds.Any()){
                    continue;
                }
            }
            if(discount is{HasGiftProducts: true, GiftProductIds: not {Count: > 0}}){
                continue;
            }
            discountsToValidate.Add(discount);
        }
        
        // Optimize: Validate rules in parallel (if discount has rule)
        var validationTasks = discountsToValidate.Select(async discount => 
        {
            if(discount.Rule == null)
                return (discount, true);
            
            var isValid = await ValidateDiscountWithRuleAsync(discount);
            return (discount, isValid);
        }).ToList();
        
        var validationResults = await Task.WhenAll(validationTasks);
        
        foreach(var (discount, isValid) in validationResults)
        {
            if(isValid)
            {
                allowedDiscounts.Add(discount);
            }
        }
        return allowedDiscounts;
    }
    private async Task<List<CompanyRate>> GetAllCompanyRatesAsync(List<CartItem> cart){
        // Use separate DbContext scope for parallel execution
        using var scope = ScopeFactory.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        var scopedCompanyRateRepo = scopedContext.GetRepository<CompanyRate>();
        
        var cartSellerIds = cart.Select(c => c.ProductSellerItem.SellerId).Distinct().ToArray();
        var cartTierIds = cart.SelectMany(c => c.Product.ProductTiers.Select(t => t.TierId)).Distinct().ToArray();
        var cartCategoryIds = cart.SelectMany(c => c.Product.Categories.Select(t => t.CategoryId)).Distinct().ToArray();
        var cartProductIds = cart.Select(c => c.ProductId).Distinct().ToArray();
        return await scopedCompanyRateRepo.GetAll(predicate:null, disableTracking:true)
            .Where(r => r.Status == (int) EntityStatus.Active)
            .Where(r => r.CompanyId.HasValue && cartSellerIds.Contains(r.CompanyId.Value))
            .Where(r => (r.TierId.HasValue && cartTierIds.Contains(r.TierId.Value)) 
                || (r.CategoryId.HasValue && cartCategoryIds.Contains(r.CategoryId.Value)) 
                || (r.ProductId.HasValue && cartProductIds.Contains(r.ProductId.Value)))
            .ToListAsync();
    }
    public async Task<bool> ValidateDiscountWithRuleAsync(Discount discount){
        try{
            if(discount.Rule == null){
                return true;
            }
            using(await RuleEngineRepository.BuildAsync(discount.Rule, DiscountFieldDefinitions.Scope)){
                var factory = RuleEngineRepository.Compile();
                var session = factory.CreateSession();
                session.Fire();
                var ruleEngineResult = RuleEngineRepository.GetResult();
                return ruleEngineResult.IsValid;
            }
        } catch(Exception ex){
            // If rule engine field scopes are not registered yet, avoid blocking cart
            // and consider the discount as valid by default.
            try{ Console.WriteLine(ex); } catch {}
            return true;
        }
    }
    public List<Discount> GetPreferredDiscount(IList<Discount> discounts, decimal amount, out decimal discountAmount){
        var result = new List<Discount>();
        discountAmount = decimal.Zero;
        if(!discounts.Any()){
            return result;
        }
        foreach(var discount in discounts){
            var currentDiscountValue = GetDiscountAmount(discount, amount);
            if(currentDiscountValue <= discountAmount){
                continue;
            }
            discountAmount = currentDiscountValue;
            result.Clear();
            result.Add(discount);
        }
        var cumulativeDiscounts = discounts.Where(x => x.IsCumulative).OrderBy(x => x.Name).ToList();
        if(cumulativeDiscounts.Count <= 1){
            return result;
        }
        var cumulativeDiscountAmount = cumulativeDiscounts.Sum(d => GetDiscountAmount(d, amount));
        if(cumulativeDiscountAmount <= discountAmount){
            return result;
        }
        discountAmount = cumulativeDiscountAmount;
        result.Clear();
        result.AddRange(cumulativeDiscounts);
        return result;
    }
    public decimal GetDiscountAmount(Discount discount, decimal amount){
        decimal result;
        if(discount.UsePercentage){
            var pct = discount.DiscountPercentage ?? 0m;
            result = Math.Round(amount * pct / 100m, 2, MidpointRounding.AwayFromZero);
        } else{
            result = discount.DiscountAmount ?? 0m;
        }
        if(discount is { UsePercentage: true, MaximumDiscountAmount: not null } && result > discount.MaximumDiscountAmount.Value){
            result = discount.MaximumDiscountAmount.Value;
        }
        if(result < 0m){
            result = 0m;
        }
        return result;
    }
    public async Task<decimal> GetAppMinimumCartTotal(){
        return await Cache.GetOrCreateAsync("AppSettings:MinCartTotal", async entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                var minCartTotalSetting = await Context.GetRepository<AppSettings>().GetFirstOrDefaultAsync(predicate:x => x.Key == "MinCartTotal");
                return minCartTotalSetting != null ? Convert.ToDecimal(minCartTotalSetting.Value) : 0;
            }
        );
    }
}
