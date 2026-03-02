namespace Remar.Dtos;
public class RemarProductDto
{
    public int ProductId{get;set;}
    public string Code { get; set; }
    public string Cross_Referans { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("Depo_1")]
    public string Depo_1 { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("Depo-1")]
    public string Depo1 { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("Depo_2")]
    public string Depo_2 { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("Depo-2")]
    public string Depo2 { get; set; }

    public string Manufacturer { get; set; }
    public int MinOrderQuantity { get; set; }
    public string Name { get; set; }
    public string Oem_No { get; set; }
    public string PackageUsage { get; set; }
    public decimal SalePriceContact { get; set; }
    public string SalePriceContactCurrency { get; set; }
    public string SpecialField_1 { get; set; }
    public string SpecialField_2 { get; set; }
    public string Unit { get; set; }
}