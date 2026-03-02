using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft fatura önizleme (HTML) response DTO
/// </summary>
public class OdaksoftPreviewResponseDto
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }
}
