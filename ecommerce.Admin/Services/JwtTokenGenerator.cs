using System.Text;
using Jose;
namespace ecommerce.Admin.Services;
public class JwtTokenGenerator
{
    public static string GenerateJwtToken(string apiKey, string apiSecret)
    {
        var payload = new Dictionary<string, object>
        {
            { "iss", apiKey },
            { "exp", DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds() }
        };

        return JWT.Encode(payload, Encoding.UTF8.GetBytes(apiSecret), JwsAlgorithm.HS256);
    }
}