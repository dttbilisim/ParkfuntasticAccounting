namespace ecommerce.Iyzico.Payment.Dtos
{
    public class PaymentCallBackRequest
    {
        public string status { get; set; }
        public string paymentId { get; set; }
        public string conversationData { get; set; }
        public long conversationId { get; set; }
        public string mdStatus { get; set; }
    }
}

