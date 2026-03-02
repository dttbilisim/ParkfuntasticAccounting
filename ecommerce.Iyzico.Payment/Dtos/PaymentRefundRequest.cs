namespace ecommerce.Iyzico.Payment.Dtos
{
    public class PaymentRefundRequest
    {
        public string locale { get; set; }
        public string conversationId { get; set; }
        public string paymentTransactionId { get; set; }
        public double price { get; set; }
        public string ip { get; set; }
        public string currency { get; set; }
    }
}

