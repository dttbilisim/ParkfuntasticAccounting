using System.Text.Json.Serialization;
namespace Telecom.Address.Dtos;
public class ApiResponse<T>
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("data")]
    public T Data { get; set; }
}
