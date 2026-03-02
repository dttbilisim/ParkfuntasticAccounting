using System.Text.Json.Serialization;
namespace BasbugOto.Dtos;
public class BasbugGroupResponseDto
{
    [JsonPropertyName("malzemeGruplariListesi")]
    public List<BasbugGroupDto> MalzemeGruplariListesi { get; set; } = new();
}

public class BasbugGroupDto
{
    [JsonPropertyName("kod")]
    public string Kod { get; set; }

    [JsonPropertyName("ad")]
    public string Ad { get; set; }
}