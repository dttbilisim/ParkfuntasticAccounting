namespace ecommerce.Admin.Domain.Dtos.ReportDto;
public class ProductAdvertDto{
    public string ProductName{get;set;}
    public string Barcode{get;set;}
    public int AdvertCount{get;set;}
    public decimal AvgPrice{get;set;}
    public decimal MaxPrice{get;set;}
    public decimal MinPrice{get;set;}
    public decimal RetailPrice{get;set;}
    
}
