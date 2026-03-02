using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft DownloadOutboxInvoice API response DTO
/// </summary>
public class DownloadOutboxInvoiceResponseDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public bool Status { get; set; }

    /// <summary>
    /// Base64 encoded PDF byte array
    /// </summary>
    [JsonPropertyName("byteArray")]
    public string? ByteArray { get; set; }

    /// <summary>
    /// Fatura HTML içeriği
    /// </summary>
    [JsonPropertyName("html")]
    public string? Html { get; set; }

    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }
}
