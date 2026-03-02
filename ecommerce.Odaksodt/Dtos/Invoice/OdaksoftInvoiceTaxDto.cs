using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft fatura vergi DTO
/// </summary>
public class OdaksoftInvoiceTaxDto
{
    /// <summary>
    /// Vergi adı (KDV, ÖTV, vb.)
    /// </summary>
    [JsonPropertyName("taxName")]
    public string TaxName { get; set; } = "KDV";

    /// <summary>
    /// Vergi kodu
    /// </summary>
    [JsonPropertyName("taxCode")]
    public string TaxCode { get; set; } = "0015"; // 0015 = KDV

    /// <summary>
    /// Vergi oranı (%)
    /// </summary>
    [JsonPropertyName("taxRate")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Vergi tutarı
    /// </summary>
    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Vergi muafiyet nedeni kodu
    /// </summary>
    [JsonPropertyName("taxExemptionReasonCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TaxExemptionReasonCode { get; set; }
}
