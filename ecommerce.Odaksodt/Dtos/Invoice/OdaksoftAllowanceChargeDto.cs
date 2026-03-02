using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft iskonto/artırım DTO
/// </summary>
public class OdaksoftAllowanceChargeDto
{
    /// <summary>
    /// Oran (%)
    /// </summary>
    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    /// <summary>
    /// Tutar
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Açıklama
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}
