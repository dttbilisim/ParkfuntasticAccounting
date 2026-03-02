namespace ecommerce.Iyzico.Payment.Dtos {
    public class Payment3DHtmlRequest {
        public string locale { get; set; }
        public string conversationId { get; set; }
        public string paymentId { get; set; }
        public string conversationData { get; set; }
        public string errorCode { get; set; }
        public string errorMessage { get; set; }
        public string token {get; set;}
    }
}

