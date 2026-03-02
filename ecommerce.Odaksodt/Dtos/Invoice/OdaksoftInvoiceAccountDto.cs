using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft fatura hesap bilgileri (müşteri) DTO
/// </summary>
public class OdaksoftInvoiceAccountDto
{
    /// <summary>
    /// VKN veya TCKN
    /// </summary>
    [JsonPropertyName("vknTckn")]
    public string VknTckn { get; set; } = string.Empty;

    /// <summary>
    /// Hesap adı (müşteri unvanı)
    /// </summary>
    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Vergi dairesi adı
    /// </summary>
    [JsonPropertyName("taxOfficeName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TaxOfficeName { get; set; }

    /// <summary>
    /// Ülke adı
    /// </summary>
    [JsonPropertyName("countryName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CountryName { get; set; }

    /// <summary>
    /// Şehir adı
    /// </summary>
    [JsonPropertyName("cityName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CityName { get; set; }

    /// <summary>
    /// Sokak adı
    /// </summary>
    [JsonPropertyName("streetName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StreetName { get; set; }

    /// <summary>
    /// İlçe
    /// </summary>
    [JsonPropertyName("district")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? District { get; set; }

    /// <summary>
    /// İlçe/Semt (citySubdivision - API tarafından zorunlu)
    /// </summary>
    [JsonPropertyName("citySubdivision")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CitySubdivision { get; set; }

    /// <summary>
    /// Telefon
    /// </summary>
    [JsonPropertyName("telephone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Telephone { get; set; }

    /// <summary>
    /// E-posta
    /// </summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }
}
