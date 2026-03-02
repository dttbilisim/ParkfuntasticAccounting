namespace ecommerce.Admin.Domain.Dtos.ProductDto;
public class ProductAdvertListDto{
    public string AccountName{get;set;}
    public int Stock { get; set; }
    public decimal Price { get; set; }
    public DateTime ExprationDate { get; set; }
    public string Status{get;set;}
}
