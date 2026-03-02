using System.Text.Json.Serialization;

namespace ecommerce.Web.Domain.Dtos.Address
{
    public class StreetDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";
        
        [JsonPropertyName("value")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("neighboor_id")]
        public int NeighboorId { get; set; }
    }
}
