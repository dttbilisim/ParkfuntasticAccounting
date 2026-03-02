using System.Text.Json.Serialization;

namespace ecommerce.Web.Domain.Dtos.Address
{
    public class BuildingDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";
        
        [JsonPropertyName("value")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("street_id")]
        public int StreetId { get; set; }
    }
}
