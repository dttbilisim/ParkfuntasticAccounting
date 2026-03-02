using System.Text.Json.Serialization;
namespace BasbugOto.Dtos;
public class BasbugTokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
}