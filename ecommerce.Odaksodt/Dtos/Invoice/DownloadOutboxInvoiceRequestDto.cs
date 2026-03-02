using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft DownloadOutboxInvoice API request DTO
/// </summary>
public class DownloadOutboxInvoiceRequestDto
{
    [JsonPropertyName("ettnList")]
    public List<string> EttnList { get; set; } = new();

    [JsonPropertyName("isDefaultXslt")]
    public bool IsDefaultXslt { get; set; } = true;

    [JsonPropertyName("companyIdentifier")]
    public string? CompanyIdentifier { get; set; }
}
