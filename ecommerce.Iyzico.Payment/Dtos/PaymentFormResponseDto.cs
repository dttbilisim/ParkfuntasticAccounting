namespace ecommerce.Iyzico.Payment.Dtos;
public class PaymentFormResponseDto{
    
    
    public string token{get;set;}
    public decimal price{get;set;}
    public string buyerName{get;set;}
    public string buyerSurname{get;set;}
    public string email{get;set;}
    public string status{get;set;}
    public string errorMessage {get;set;}
    
}
