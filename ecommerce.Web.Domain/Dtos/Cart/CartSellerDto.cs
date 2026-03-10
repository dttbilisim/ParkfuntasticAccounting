namespace ecommerce.Web.Domain.Dtos.Cart;
public class CartSellerDto
{
    public int SellerId { get; set; }

    public string SellerName { get; set; } = null!;

    public bool IsBlueTick{get;set;}

    public decimal SellerPoint { get; set; }

    public string ? SellerAddress{get;set;}

    public string ? SellerEmail{get;set;}

    public string SellerPhoneNumber{get;set;}
    public string ? SellerTaxName{get;set;}
  
    public string ? SellerTaxNumber{get;set;}

    public decimal MinCartTotal { get; set; }

    public List<CartItemDto> Items { get; set; } = new();

    public decimal SubTotal { get; set; }

    public decimal OrderTotal { get; set; }

    public decimal OrderTotalDiscount { get; set; }

    public decimal CargoPrice { get; set; }

    public decimal CargoDiscount { get; set; }

    public int TotalItems { get; set; }

    public CartCargoDto? SelectedCargo { get; set; }

    public List<CartCargoDto> Cargoes { get; set; } = new();

    public string? ProfilePhotoUrl { get; set; }

  

    public decimal Desi { get; set; }

    public bool IsAllItemsPassive { get; set; }

    public List<string> Warnings { get; set; } = new();
    public List<CartAppliedDiscountDto> AppliedDiscounts { get; set; } = new();

    /// <summary>Para birimi kodu (USD, TRY, EUR vb.) - ilk üründen alınır.</summary>
    public string? Currency { get; set; }
}

public class CartCargoDto
{
    public int CargoId { get; set; }
    public int CompanyCargoId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CargoPrice { get; set; }
    public decimal CargoOverloadPrice { get; set; }
    public decimal MinBasketAmount { get; set; }
    public bool IsDefault { get; set; }
    public bool IsLocalStorage { get; set; }
    /// <summary>Admin paneldeki Kargo Mesajı — mobilde kargo adı altında açıklama olarak gösterilir.</summary>
    public string? Message { get; set; }
    /// <summary>Kargo tipi: 0 = Standard, 1 = BicopsExpress. Mobilde harita/hızlı teslimat için kullanılır.</summary>
    public int CargoType { get; set; }
    public CartCargoPropertyDto? SelectedProperty { get; set; }
    public List<CartCargoPropertyDto> Properties { get; set; } = new();
    /// <summary>Sadece CargoType=BicopsExpress: Tek ücretle kapsanan km.</summary>
    public decimal CoveredKm { get; set; }
    /// <summary>Sadece CargoType=BicopsExpress: Kapsanan km sonrası km başı ek ücret (TL).</summary>
    public decimal PricePerExtraKm { get; set; }
    /// <summary>Sadece CargoType=BicopsExpress: Tek ücret (base fee).</summary>
    public decimal BaseFeeAmount { get; set; }
}

public class CartCargoPropertyDto
{
    public string Size { get; set; } = string.Empty;
    public int DesiMinValue { get; set; }
    public int DesiMaxValue { get; set; }
    public decimal Price { get; set; }
}
