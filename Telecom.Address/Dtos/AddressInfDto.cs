using System.Text.Json.Serialization;

namespace Telecom.Address.Dtos
{
    public class AddressInfDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; } = "";

        [JsonPropertyName("home_id")]
        public int HomeId { get; set; }
    }
}
