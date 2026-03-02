using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Common;

/// <summary>
/// Odaksoft API hata response DTO - tüm endpoint'lerin ortak hata formatı
/// API Response: {"data":null,"status":false,"message":"...","exceptionMessage":"..."}
/// </summary>
public class OdaksoftErrorResponseDto
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }
}
