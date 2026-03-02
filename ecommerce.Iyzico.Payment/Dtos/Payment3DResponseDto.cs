namespace ecommerce.Iyzico.Payment.Dtos {
    public class Payment3DResponseDto {

        public string status { get; set; }
        public string locale { get; set; }
        public long systemTime { get; set; }
     
        public string conversationId { get; set; }
        public string threeDSHtmlContent { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }

    }
}

