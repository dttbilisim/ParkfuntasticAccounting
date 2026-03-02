namespace ecommerce.Web.Domain.Dtos.Order;

public class CreditCardDto
{
    public string CardNumber { get; set; }
    
    public string ExpireYear { get; set; }
    
    public string ExpireMonth { get; set; }
    
    public string CardHolderName { get; set; }
    
    public string Cvc{get;set;}
    public string ? CardAlias{get;set;}
    
    public string ? CardUserKey{get;set;}
    public string ? CardToken{get;set;}
}