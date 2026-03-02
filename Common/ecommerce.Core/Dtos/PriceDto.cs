using System.Text.Json.Serialization;

namespace ecommerce.Core.Dtos;
public class PriceDto
{
    [JsonPropertyName("ParaBirimi")]
    public string? ParaBirimi { get; set; }
    
    [JsonPropertyName("Deger")]
    public decimal? Deger { get; set; }
}

