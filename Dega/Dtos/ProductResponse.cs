using System.Text.Json.Serialization;
namespace Dega.Dtos;
public class ProductResponse
{
    [JsonPropertyName("Code")]
    public string Code { get; set; }

    [JsonPropertyName("Depo-1")]
    public string Depo1 { get; set; }

    [JsonPropertyName("Depo-2")]
    public string Depo2 { get; set; }

    [JsonPropertyName("Depo-3")]
    public string Depo3 { get; set; }

    [JsonPropertyName("Depo-4")]
    public string Depo4 { get; set; }

    [JsonPropertyName("Depo-5")]
    public string Depo5 { get; set; }

    [JsonPropertyName("Depo-6")]
    public string Depo6 { get; set; }

    [JsonPropertyName("Depo_1")]
    public string Depo_1 { get; set; }

    [JsonPropertyName("Depo_2")]
    public string Depo_2 { get; set; }

    [JsonPropertyName("Depo_3")]
    public string Depo_3 { get; set; }

    [JsonPropertyName("Depo_4")]
    public string Depo_4 { get; set; }

    [JsonPropertyName("Depo_5")]
    public string Depo_5 { get; set; }

    [JsonPropertyName("Depo_6")]
    public string Depo_6 { get; set; }

    [JsonPropertyName("Manufacturer")]
    public string Manufacturer { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Orjinal_Kod")]
    public string OrjinalKod { get; set; }

    [JsonPropertyName("SalePriceContact")]
    public decimal SalePriceContact { get; set; }

    [JsonPropertyName("SalePriceContactCurrency")]
    public string SalePriceContactCurrency { get; set; }

    [JsonPropertyName("SpecialField_9")]
    public string SpecialField9 { get; set; }

    [JsonPropertyName("Unit")]
    public string Unit { get; set; }
}
