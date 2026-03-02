using System.Text.Json.Serialization;
namespace ecommerce.Admin.Domain.Dtos.InBoxDto
{
    public class InboxTokenUserDto
    {
        [JsonPropertyName("EmailAddress")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("Password")]
        public string Password { get; set; }
    }
}
