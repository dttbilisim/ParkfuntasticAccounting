namespace ecommerce.Cargo.Mng.Models;

public class CreateOrderResponse
{
    public string  orderInvoiceId { get; set; }
    
    public string  orderInvoiceDetailId { get; set; } 
    
    public string  shipperBranchCode { get; set; }
    public CreateOrderResponseError Error{get;set;}
   
}

public class CreateOrderResponseError{
    public string ? Code{get;set;}
    public string ? Message{get;set;}
    public string ? Description{get;set;}
}
