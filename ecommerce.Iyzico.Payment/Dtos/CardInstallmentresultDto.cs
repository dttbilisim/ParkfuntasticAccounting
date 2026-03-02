namespace ecommerce.Iyzico.Payment.Dtos
{
    public class CardInstallmentresultDto
    {
        public class InstallmentDetail {
            public string binNumber { get; set; }
            public int price { get; set; }
            public string cardType { get; set; }
            public string cardAssociation { get; set; }
            public string cardFamilyName { get; set; }
            public int force3ds { get; set; }
            public int bankCode { get; set; }
            public string bankName { get; set; }
            public int forceCvc { get; set; }
            public List<InstallmentPrice> installmentPrices { get; set; }
        }

        public class InstallmentPrice {
            public double installmentPrice { get; set; }
            public double totalPrice { get; set; }
            public int installmentNumber { get; set; }
        }

        public class CardInstallmentresultListDto {
            public string status { get; set; }
            public string locale { get; set; }
            public long systemTime { get; set; }
            public string conversationId { get; set; }
            public List<InstallmentDetail> installmentDetails { get; set; }
        }
    }
}

