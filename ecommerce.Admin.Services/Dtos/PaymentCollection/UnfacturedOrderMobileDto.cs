using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.PaymentCollection;

/// <summary>
/// Faturalaşmamış sipariş DTO'su (mobil için sadeleştirilmiş)
/// </summary>
public class UnfacturedOrderMobileDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public decimal GrandTotal { get; set; }
    public OrderStatusType OrderStatusType { get; set; }
    public int ItemCount { get; set; }
}
