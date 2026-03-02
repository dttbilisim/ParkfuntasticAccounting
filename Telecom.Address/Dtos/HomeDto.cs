using System.Text.Json.Serialization;

namespace Telecom.Address.Dtos
{
    public class HomeDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("value")]
        public string Name { get; set; } = "";

        [JsonPropertyName("building_id")]
        public int BuildingId { get; set; }
    }
}
