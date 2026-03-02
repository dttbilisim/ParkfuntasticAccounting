namespace ecommerce.Iyzico.Payment.Dtos
{
    public class SubMerhantApprovePaymentResponse
    {
        public string status { get; set; }
        public string paymentTransactionId { get; set; }
        public string errorCode { get; set; }
        public string errorMessage { get; set; }

    }
}

