using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Fatura kalemi DTO
/// </summary>
public class InvoiceLineDto
{
    /// <summary>
    /// Ürün/hizmet adı
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Miktar
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Birim fiyat
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// KDV oranı (%)
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// İskonto oranı (%)
    /// </summary>
    public decimal? DiscountRate { get; set; }

    /// <summary>
    /// Birim
    /// </summary>
    public string Unit { get; set; } = "Adet";

    /// <summary>
    /// Ürün kodu (boşsa JSON'a dahil edilmez)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductCode { get; set; }
}
