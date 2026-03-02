using System.Text.Json.Serialization;

namespace ecommerce.Web.Domain.Dtos.Address
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }
        
        [JsonPropertyName("data")]
        public T? Data { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
