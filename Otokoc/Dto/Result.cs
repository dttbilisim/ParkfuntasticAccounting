using System.Xml.Serialization;
namespace Otokoc.Dto;
[XmlRoot("Result")]
public class Result<T>
{
    public T ResultObject { get; set; }
}

public class ProductList
{
    [XmlElement("Product")]
    public List<Product> Products { get; set; }

    [XmlElement("EndOfProducts")]
    public bool EndOfProducts { get; set; }
}

public class Product
{
    [XmlElement("ProductCode")]
    public string ProductCode { get; set; }

    [XmlElement("ProductName")]
    public string ProductName { get; set; }

    [XmlElement("BrandCode")]
    public string BrandCode { get; set; }

    [XmlElement("BrandName")]
    public string BrandName { get; set; }

    [XmlElement("Price")]
    public decimal Price { get; set; }

    [XmlElement("Currency")]
    public string Currency { get; set; }

    [XmlElement("StockQuantity")]
    public int StockQuantity { get; set; }

    [XmlElement("TaxRate")]
    public decimal TaxRate { get; set; }

    [XmlElement("Barcode")]
    public string Barcode { get; set; }

    [XmlElement("OEM")]
    public string OEM { get; set; }

    [XmlElement("GroupCode")]
    public string GroupCode { get; set; }

    [XmlElement("GroupName")]
    public string GroupName { get; set; }

    [XmlIgnore]
    public string ImageUrl => $"https://b2b.otokoc.com.tr:5568/XmlService/DownloadProductImage/ByCode/{ProductCode}";
}