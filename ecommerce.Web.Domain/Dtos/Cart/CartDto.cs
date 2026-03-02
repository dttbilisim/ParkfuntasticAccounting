namespace ecommerce.Web.Domain.Dtos.Cart;
public class CartDto
{
    public decimal SubTotal { get; set; }

    public decimal OrderTotal { get; set; }

    public decimal OrderTotalDiscount { get; set; }

    public decimal CargoPrice { get; set; }

    public decimal CargoDiscount { get; set; }

    public int TotalItems{get;set;} = 0;

    public int CartCount{get;set;} = 0;

    public bool IsCouponCodeApplied { get; set; }

    public string? AppliedCouponCode { get; set; }

    public decimal? MaturityDifference{get;set;}
 

    public List<CartSellerDto> Sellers { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<CartAppliedDiscountDto> AppliedDiscounts { get; set; } = new();

    public List<CartDiscountSummaryDto> DiscountSummaries { get; set; } = new();
}

public class CartDiscountSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
    public decimal Amount { get; set; }
    
}
