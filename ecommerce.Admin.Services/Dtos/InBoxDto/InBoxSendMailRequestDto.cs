using System.Text.Json.Serialization;
namespace ecommerce.Admin.Domain.Dtos.InBoxDto
{
    public class InBoxSendMailRequestDto
    {
        [JsonPropertyName("from")]
        public From from { get; set; }

        [JsonPropertyName("to")]
        public List<To> to { get; set; }

        [JsonPropertyName("subject")]
        public string subject { get; set; }

        [JsonPropertyName("htmlContent")]
        public string htmlContent { get; set; }
         
    }
 
   
    public class From
    {
        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("email")]
        public string email { get; set; }
    }
     
    public class To
    {
        [JsonPropertyName("email")]
        public string email { get; set; }
    }


}
