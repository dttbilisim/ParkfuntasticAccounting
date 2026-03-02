namespace ecommerce.Core.Entities;

using ecommerce.Core.Utils;

public class CartResult
{
    public decimal SubTotal { get; set; }

    public decimal OrderTotal { get; set; }

    public decimal OrderTotalDiscount { get; set; }

    public decimal CargoPrice { get; set; }

    public decimal CargoDiscount { get; set; }

    public int TotalItems { get; set; }

    public int CartCount { get; set; }

    public bool IsCouponCodeApplied { get; set; }

    public string? AppliedCouponCode { get; set; }

    public int? AppliedCompanyCouponCodeId { get; set; }

    public List<CartSellerResult> Sellers { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<Discount> AppliedDiscounts { get; set; } = new();

    // Aggregated summaries for UI consumption
    public List<DiscountSummary> DiscountSummaries { get; set; } = new();
}

public class DiscountSummary
{
    public string Name { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
    public decimal Amount { get; set; }
}

public class CartSellerResult
{
    public int SellerId { get; set; }
    public bool IsBlueTick{get;set;}

    public string SellerName { get; set; } = null!;

    public decimal SellerPoint { get; set; }
    public string ? SellerAddress{get;set;}

    public string ? SellerEmail{get;set;}

    public string SellerPhoneNumber{get;set;}

    public string ? SellerTaxName{get;set;}

    public string ? SellerTaxNumber{get;set;}

    public string? IyzicoSubmerhantKey { get; set; }

    public decimal MinCartTotal { get; set; }

    public List<CartItem> Items { get; set; } = new();

    public decimal SubTotal { get; set; }

    public decimal OrderTotal { get; set; }

    public decimal OrderTotalDiscount { get; set; }

    public decimal CargoPrice { get; set; }

    public decimal CargoDiscount { get; set; }

    public int TotalItems { get; set; }

    public CartCargoResult? SelectedCargo { get; set; }

    public List<CartCargoResult> Cargoes { get; set; } = new();

    public decimal Desi { get; set; }

    public bool IsAllItemsPassive { get; set; }

    public List<string> Warnings { get; set; } = new();

    public List<Discount> AppliedDiscounts { get; set; } = new();
}

public class CartCargoResult
{
    public int CargoId { get; set; }

    public int CompanyCargoId { get; set; }

    public string Name { get; set; } = null!;

    public decimal CargoPrice { get; set; }

    public decimal CargoOverloadPrice { get; set; }

    public decimal MinBasketAmount { get; set; }

    public bool IsDefault { get; set; }
    public bool IsLocalStorage{get;set;}

    /// <summary>Admin paneldeki Kargo Mesajı / açıklama — mobilde kargo adı altında küçük gösterilir.</summary>
    public string? Message { get; set; }

    public CartCargoPropertyResult? SelectedProperty { get; set; }

    public List<CartCargoPropertyResult> Properties { get; set; } = new();

    /// <summary>Kargo tipi: Standard = 0, BicopsExpress = 1. Mobilde harita/hızlı teslimat için kullanılır.</summary>
    public CargoType CargoType { get; set; } = CargoType.Standard;

    /// <summary>Sadece CargoType=BicopsExpress: Tek ücretle kapsanan km.</summary>
    public decimal CoveredKm { get; set; }

    /// <summary>Sadece CargoType=BicopsExpress: Kapsanan km sonrası km başı ek ücret (TL).</summary>
    public decimal PricePerExtraKm { get; set; }

    /// <summary>Sadece CargoType=BicopsExpress: Tek ücret (Amount). Modal'da "base fee" olarak gösterilir.</summary>
    public decimal BaseFeeAmount { get; set; }
}

public class CartCargoPropertyResult
{
    public string Size { get; set; } = null!;

    public int DesiMinValue { get; set; }

    public int DesiMaxValue { get; set; }

    public decimal Price { get; set; }
}

public class CartCustomerSavedPreferences
{
    public string? UsedCouponCode { get; set; }

    public Dictionary<int, int> SelectedCargoes { get; set; } = new();

    public List<int> IgnoredGiftProducts { get; set; } = new();

    /// <summary>Satıcı bazlı teslimat mesafesi (km). CargoType=BicopsExpress için kargo ücreti hesaplamasında kullanılır. Key=SellerId, Value=mesafe km.</summary>
    public Dictionary<int, double>? DistanceKmBySellerId { get; set; }
}