using Newtonsoft.Json;

namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailCartItemDto
{
    public string? StokKodu { get; set; }
    public int Adet { get; set; }
}

public class OISMLServiceResultDto
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("message")]
    public object? Message { get; set; }
    
    [JsonProperty("rowcount")]
    public int RowCount { get; set; }
    
    [JsonProperty("duration")]
    public double Duration { get; set; }
}
