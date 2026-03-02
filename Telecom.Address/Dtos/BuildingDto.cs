using System.Text.Json.Serialization;
namespace Telecom.Address.Dtos
{
    public class BuildingDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("value")]
        public string Name { get; set; } = "";

        [JsonPropertyName("street_id")]
        public int StreetId { get; set; }
    }
}
