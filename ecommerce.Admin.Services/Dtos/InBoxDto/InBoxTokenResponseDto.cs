using System.Text.Json.Serialization;
namespace ecommerce.Admin.Domain.Dtos.InBoxDto
{
    public class InBoxTokenResponseDto
    {
        [JsonPropertyName("version")]
        public string version { get; set; }

        [JsonPropertyName("resultStatus")]
        public bool resultStatus { get; set; }

        [JsonPropertyName("resultCode")]
        public int resultCode { get; set; }

        [JsonPropertyName("resultMessage")]
        public string resultMessage { get; set; }

        [JsonPropertyName("resultObject")]
        public ResultObject resultObject { get; set; }
    }
 
    public class ResultObject
    {
        [JsonPropertyName("access_token")]
        public string access_token { get; set; }

        [JsonPropertyName("expires_in")]
        public int expires_in { get; set; }

        [JsonPropertyName("token_type")]
        public string token_type { get; set; }

        [JsonPropertyName("refresh_token")]
        public string refresh_token { get; set; }
    }
 
}
