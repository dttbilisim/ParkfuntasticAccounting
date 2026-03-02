using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Gib;

/// <summary>
/// GİB kullanıcı tipi sorgulama request DTO
/// </summary>
public class CheckUserRequestDto
{
    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = "Invoice";

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = "";
}
