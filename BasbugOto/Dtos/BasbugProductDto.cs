using System.Text.Json.Serialization;
namespace BasbugOto.Dtos;
public class BasbugProductResponseDto
{
    [JsonPropertyName("malzemeListesi")]
    public List<BasbugProductDto> MalzemeListesi { get; set; } = new();
}

public class BasbugProductDto
{
    [JsonPropertyName("no")]
    public string No { get; set; }

    [JsonPropertyName("ac")]
    public string Aciklama1 { get; set; }

    [JsonPropertyName("ac2")]
    public string Aciklama2 { get; set; }

    [JsonPropertyName("mkk")]
    public string MarkaKod { get; set; }

    [JsonPropertyName("oe")]
    public string OemKod { get; set; }

    [JsonPropertyName("uk")]
    public string Uretici { get; set; }

    [JsonPropertyName("lgk")]
    public string GrupKod { get; set; }

    [JsonPropertyName("m")]
    public string Model { get; set; }

    [JsonPropertyName("mo")]
    public string Motor { get; set; }

    [JsonPropertyName("y")]
    public string Yil { get; set; }

    [JsonPropertyName("b")]
    public string Birim { get; set; }

    [JsonPropertyName("dc")]
    public string ParaBirimi { get; set; }

    [JsonPropertyName("lf")]
    public decimal Fiyat { get; set; }
}