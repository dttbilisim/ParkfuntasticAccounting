using System.Text.Json.Serialization;
namespace Telecom.Address.Dtos
{
    public class NeighboorDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("value")]
        public string Name { get; set; } = "";

        [JsonPropertyName("town_id")]
        public int TownId { get; set; }
    }
}
