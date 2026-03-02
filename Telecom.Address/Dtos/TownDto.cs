using System.Text.Json.Serialization;

namespace Telecom.Address.Dtos
{
    public class TownDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("value")]
        public string Name { get; set; } = "";

        [JsonPropertyName("city_id")]
        public int CityId { get; set; }
    }
}
