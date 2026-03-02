using System.Text.Json.Serialization;
namespace Telecom.Address.Dtos
{
    public class CityDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("value")]
        public string Name { get; set; } = "";
    }
}
