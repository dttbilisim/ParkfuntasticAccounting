using Newtonsoft.Json;

namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OISMLResultDto
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("ok")] // Just in case they use 'ok' in some endpoints
    public bool Ok { get { return Success; } set { Success = value; } }
    
    [JsonProperty("message")]
    public object? Message { get; set; }
    
    [JsonProperty("rowcount")]
    public int RowCount { get; set; }
    
    [JsonProperty("duration")]
    public double Duration { get; set; }
}
