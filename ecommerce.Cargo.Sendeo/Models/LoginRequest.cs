using Newtonsoft.Json;

namespace ecommerce.Cargo.Sendeo.Models
{
    public class LoginRequest
    {
        [JsonProperty("musteri")]
        public string CustomerName { get; set; } = null!;

        [JsonProperty("sifre")]
        public string Password { get; set; } = null!;
    }
}