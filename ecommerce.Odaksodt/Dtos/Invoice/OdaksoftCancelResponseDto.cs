using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft fatura iptal response DTO
/// API Response: {"data":null,"status":false,"message":"...","exceptionMessage":"..."}
/// </summary>
public class OdaksoftCancelResponseDto
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
