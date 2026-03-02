namespace ecommerce.Iyzico.Payment.Dtos
{
    public class PaymentCallBackResponse
    {
        public string locale { get; set; }
        public string conversationId { get; set; }
        public string paymentId { get; set; }
        public string conversationData { get; set; }
        public string errorCode { get; set; }
        public string errorMessage { get; set; }
    }
}

