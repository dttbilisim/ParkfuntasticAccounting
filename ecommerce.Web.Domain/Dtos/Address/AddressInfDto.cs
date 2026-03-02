using System.Text.Json.Serialization;

namespace ecommerce.Web.Domain.Dtos.Address
{
    public class AddressInfDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";
        
        [JsonPropertyName("value")]
        public string Detail { get; set; } = "";
        
        [JsonPropertyName("home_id")]
        public int HomeId { get; set; }
    }
}
