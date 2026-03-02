namespace ecommerce.Iyzico.Payment.Dtos
{
    public class CardSaveResultDto
    {
        public string status { get; set; }
        public string locale { get; set; }
        public long systemTime { get; set; }
        public string conversationId { get; set; }
        public string externalId { get; set; }
        public string email { get; set; }
        public string cardUserKey { get; set; }
        public string cardToken { get; set; }
        public string cardAlias { get; set; }
        public string binNumber { get; set; }
        public string cardType { get; set; }
        public string cardAssociation { get; set; }
        public string cardFamily { get; set; }
        public long cardBankCode { get; set; }
        public string cardBankName { get; set; }
    }
}

