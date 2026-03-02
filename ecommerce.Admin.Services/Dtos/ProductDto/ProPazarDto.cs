using System.ComponentModel;
using System.Text.Json.Serialization;
namespace ecommerce.Admin.Domain.Dtos.ProductDto;
public class ProPazarDto{
    public class Attribute
    {
        [JsonPropertyName("Name")]
        public string ? Name { get; set; }

        [JsonPropertyName("Value")]
        public string? Value { get; set; }
    }

    public class Attributes
    {
        [JsonPropertyName("Attribute")]
        public Attribute Attribute { get; set; }
    }

    public class Description
    {
        [JsonPropertyName("#cdata-section")]
        public string ? cdatasection { get; set; }
    }

    public class Images
    {
        [JsonPropertyName("Url")]
        public object ? Url { get; set; }
    }

    public class PraXmlProducts
    {
        [JsonPropertyName("@xmlns:xsi")]
        public string xmlnsxsi { get; set; }

        [JsonPropertyName("Product")]
        public List<Product> Product { get; set; }
    }

    public class Product
    {
        [JsonPropertyName("Name")]
        public string ? Name { get; set; }

        [JsonPropertyName("StockCode")]
        public string ? StockCode { get; set; }

        [JsonPropertyName("Images")]
        public Images ? Images { get; set; }

        [JsonPropertyName("Category")]
        public string ? Category { get; set; }

        [JsonPropertyName("Description")]
        public Description Description { get; set; }

        [JsonPropertyName("Barcode")]
        public string Barcode { get; set; }

        [JsonPropertyName("SalePrice")]
        public decimal? SalePrice { get; set; }

        [JsonPropertyName("VatRate")]
        [JsonConverter(typeof(DecimalConverter))]
        public decimal VatRate { get; set; }

        [JsonPropertyName("CurrencyCode")]
        public string ? CurrencyCode { get; set; }

        [JsonPropertyName("Quantity")]
        public int? Quantity { get; set; }
        
        [JsonPropertyName("ProductBrand")]
        public string ? ProductBrand { get; set; }

        [JsonPropertyName("Weight")]
        public decimal? Weight { get; set; }

        [JsonPropertyName("VariantGroups")]
        public string ? VariantGroups { get; set; }

        [JsonPropertyName("Attributes")]
        public Attributes ? Attributes { get; set; }
    }

    public class Root
    {
        [JsonPropertyName("?xml")]
        public Xml xml { get; set; }

        [JsonPropertyName("PraXmlProducts")]
        public PraXmlProducts PraXmlProducts { get; set; }
    }

    public class Xml
    {
        [JsonPropertyName("@version")]
        public string version { get; set; }

        [JsonPropertyName("@encoding")]
        public string encoding { get; set; }

        [JsonPropertyName("@standalone")]
        public string standalone { get; set; }
    }

}
