using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Gib;

/// <summary>
/// GİB kullanıcı tipi sorgulama response DTO
/// </summary>
public class CheckUserResponseDto
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public bool Data { get; set; }
}
