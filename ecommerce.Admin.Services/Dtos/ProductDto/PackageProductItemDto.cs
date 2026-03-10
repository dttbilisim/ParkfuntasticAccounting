namespace ecommerce.Admin.Domain.Dtos.ProductDto;

/// <summary>
/// Paket ürün içeriği - ürün ve fiyat bilgisi. KDV ürünün vergi oranından gelir.
/// </summary>
public class PackageProductItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    /// <summary>Fiyatın döviz tipi</summary>
    public int? CurrencyId { get; set; }
    /// <summary>Döviz kodu (TL, USD vb.) - gösterim için</summary>
    public string? CurrencyCode { get; set; }
    /// <summary>Ürünün KDV oranı (%) - sadece gösterim için</summary>
    public int TaxRate { get; set; }
}
