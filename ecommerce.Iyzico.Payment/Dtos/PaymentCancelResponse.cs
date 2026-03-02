namespace ecommerce.Iyzico.Payment.Dtos
{
    public class PaymentCancelResponse
    {
        public string ConversationId { get; set; }
        public string Status { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

    }
}

