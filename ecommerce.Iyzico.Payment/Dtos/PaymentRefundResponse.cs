namespace ecommerce.Iyzico.Payment.Dtos
{
    public class PaymentRefundResponse
    {
        public string status { get; set; }
        public string locale { get; set; }
        public long systemTime { get; set; }
        public string conversationId { get; set; }
        public string paymentId { get; set; }
        public string paymentTransactionId { get; set; }
        public decimal price { get; set; }
        public string currency { get; set; }
    }
}

