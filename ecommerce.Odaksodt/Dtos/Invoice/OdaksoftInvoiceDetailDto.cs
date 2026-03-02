using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft fatura kalemi DTO
/// </summary>
public class OdaksoftInvoiceDetailDto
{
    /// <summary>
    /// Ürün/hizmet adı
    /// </summary>
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Miktar
    /// </summary>
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Birim kodu
    /// </summary>
    [JsonPropertyName("unitCode")]
    public string UnitCode { get; set; } = "C62"; // C62 = Adet

    /// <summary>
    /// Birim fiyat
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Tutar (miktar * birim fiyat)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// KDV dahil tutar
    /// </summary>
    [JsonPropertyName("kdvDahilTutar")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? KdvDahilTutar { get; set; }

    /// <summary>
    /// Üretici ürün kodu
    /// </summary>
    [JsonPropertyName("manufacturerProductCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ManufacturerProductCode { get; set; }

    /// <summary>
    /// Alıcı ürün kodu
    /// </summary>
    [JsonPropertyName("buyerProductCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BuyerProductCode { get; set; }

    /// <summary>
    /// Ürün kodu
    /// </summary>
    [JsonPropertyName("productCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductCode { get; set; }

    /// <summary>
    /// KDV oranı (zorunlu alan - API tarafından required)
    /// </summary>
    [JsonPropertyName("vatRate")]
    public decimal VatRate { get; set; }

    /// <summary>
    /// Vergi listesi
    /// </summary>
    [JsonPropertyName("tax")]
    public List<OdaksoftInvoiceTaxDto> Tax { get; set; } = new();

    /// <summary>
    /// İskonto/artırım listesi
    /// </summary>
    [JsonPropertyName("allowanceCharge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OdaksoftAllowanceChargeDto>? AllowanceCharge { get; set; }
}
