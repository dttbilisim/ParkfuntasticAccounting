namespace ecommerce.Iyzico.Payment.Dtos
{
    public class SubMerhantCreateResultDto
    {
        public string Status { get; set; }
        public string Locale { get; set; }
        public long SystemTime { get; set; }
        public string ConversationId { get; set; }
        public string SubMerchantKey { get; set; }
    }
}

