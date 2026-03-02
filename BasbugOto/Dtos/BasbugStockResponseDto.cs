using System.Text.Json.Serialization;
namespace BasbugOto.Dtos;
public class BasbugStockResponseDto{
    [JsonPropertyName("stokListesi")]
    public List<BasbugStockDto> StokListesi { get; set; } = new();
}
