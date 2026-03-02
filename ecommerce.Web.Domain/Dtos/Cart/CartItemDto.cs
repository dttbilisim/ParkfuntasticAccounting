namespace ecommerce.Web.Domain.Dtos.Cart;
public class CartItemDto
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int ProductId { get; set; }
    public int SellerId { get; set; }

    public string ? SellerName{get;set;}

    public int ProductSellerItemId { get; set; }

    public int Quantity { get; set; }

    public string ProductName { get; set; } = null!;

    public string? PictureUrl { get; set; }

    public int Stock { get; set; }

    public DateTime ExprationDate { get; set; }

    public int MaxSellCount { get; set; }

    public int MinSellCount { get; set; }
    public decimal Step { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal UnitPriceWithoutDiscount { get; set; }

    public decimal Total { get; set; }

    public decimal TotalWithoutDiscount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal DisplaySavings { get; set; }

    public decimal ProductDesi { get; set; }

    public int Status { get; set; }

    public List<string> Warnings { get; set; } = new();

    public List<CartAppliedDiscountDto> AppliedDiscounts { get; set; } = new();

    public bool IsReadonly { get; set; }

    public bool IsGiftProduct { get; set; }

    public bool CanGiftRemove { get; set; }

    public bool IsPerfectCompatibility { get; set; }

    public List<string> PerfectCompatibilitySummaries { get; set; } = new();
}