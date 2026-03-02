namespace ecommerce.Web.Domain.Dtos.Order;

public class CheckoutResultDto
{
    public List<int>? OrderIds { get; set; }
    
    public string? IframeUrl { get; set; }
    public string ? CheckoutFormContent{get;set;}
    public List<string>? OrderNumbers { get; set; }
}