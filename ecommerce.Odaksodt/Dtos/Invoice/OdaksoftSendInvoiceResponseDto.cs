using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft fatura GİB'e gönderme response DTO
/// </summary>
public class OdaksoftSendInvoiceResponseDto
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    public bool Success => Status;
}
