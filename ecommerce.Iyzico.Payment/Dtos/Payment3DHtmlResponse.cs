namespace ecommerce.Iyzico.Payment.Dtos
{
    public class Payment3DHtmlResponse
    {
        public class ConvertedPayout {
            public decimal paidPrice { get; set; }
            public decimal iyziCommissionRateAmount { get; set; }
            public decimal iyziCommissionFee { get; set; }
            public decimal blockageRateAmountMerchant { get; set; }
            public int blockageRateAmountSubMerchant { get; set; }
            public int subMerchantPayoutAmount { get; set; }
            public decimal merchantPayoutAmount { get; set; }
            public int iyziConversionRate { get; set; }
            public int iyziConversionRateAmount { get; set; }
            public string currency { get; set; }
        }

        public class ItemTransaction {
            public string itemId { get; set; }
            public string paymentTransactionId { get; set; }
            public int transactionStatus { get; set; }
            public decimal price { get; set; }
            public decimal paidPrice { get; set; }
            public int merchantCommissionRate { get; set; }
            public decimal merchantCommissionRateAmount { get; set; }
            public decimal iyziCommissionRateAmount { get; set; }
            public decimal iyziCommissionFee { get; set; }
            public int blockageRate { get; set; }
            public decimal blockageRateAmountMerchant { get; set; }
            public int blockageRateAmountSubMerchant { get; set; }
            public string blockageResolvedDate { get; set; }
            public decimal subMerchantPrice { get; set; }
            public int subMerchantPayoutRate { get; set; }
            public decimal subMerchantPayoutAmount { get; set; }
            public decimal merchantPayoutAmount { get; set; }
            public ConvertedPayout convertedPayout { get; set; }
        }

        public class Response {
            public string status { get; set; }
            public string token { get; set; }
            public string locale { get; set; }
            public long systemTime { get; set; }
            public string conversationId { get; set; }
            public decimal price { get; set; }
            public decimal paidPrice { get; set; }
            public int installment { get; set; }
            public string paymentId { get; set; }
            public int fraudStatus { get; set; }
            public int merchantCommissionRate { get; set; }
            public decimal merchantCommissionRateAmount { get; set; }
            public decimal iyziCommissionRateAmount { get; set; }
            public decimal iyziCommissionFee { get; set; }
            public string cardType { get; set; }
            public string cardAssociation { get; set; }
            public string cardFamily { get; set; }
            public string binNumber { get; set; }
            public string basketId { get; set; }
            public string currency { get; set; }
            public List<ItemTransaction> itemTransactions { get; set; }
        }

    }
}

