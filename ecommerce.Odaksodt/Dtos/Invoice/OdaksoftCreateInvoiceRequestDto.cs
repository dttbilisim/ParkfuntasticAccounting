using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft API'nin beklediği fatura oluşturma request DTO (root)
/// </summary>
public class OdaksoftCreateInvoiceRequestDto
{
    /// <summary>
    /// Fatura listesi
    /// </summary>
    [JsonPropertyName("itemDto")]
    public List<OdaksoftInvoiceItemDto> ItemDto { get; set; } = new();

    /// <summary>
    /// Şirket tanımlayıcı
    /// </summary>
    [JsonPropertyName("companyIdentifier")]
    public string? CompanyIdentifier { get; set; }
}
