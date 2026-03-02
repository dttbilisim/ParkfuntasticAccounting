namespace ecommerce.Iyzico.Payment.Dtos
{
    public class PaymentRequestDto
    {
        public class BasketItem {
            public string id { get; set; }
            public string price { get; set; }
            public string name { get; set; }
            public string category1 { get; set; }
            public string category2 { get; set; }
            public string itemType { get; set; }
        }

        public class BillingAddress {
            public string address { get; set; }
            public string zipCode { get; set; }
            public string contactName { get; set; }
            public string city { get; set; }
            public string country { get; set; }
        }

        public class Buyer {
            public string id { get; set; }
            public string name { get; set; }
            public string surname { get; set; }
            public string identityNumber { get; set; }
            public string email { get; set; }
            public string gsmNumber { get; set; }
            public string registrationDate { get; set; }
            public string lastLoginDate { get; set; }
            public string registrationAddress { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string zipCode { get; set; }
            public string ip { get; set; }
        }

        public class PaymentCard {
            public string cardHolderName { get; set; }
            public string cardNumber { get; set; }
            public string expireYear { get; set; }
            public string expireMonth { get; set; }
            public string cvc { get; set; }
            public string CardUserKey { get; set; }
            public string CardToken { get; set; }
        }

        public class PaymentRequestRoot {
            public string locale { get; set; }
            public string conversationId { get; set; }
            public string price { get; set; }
            public string paidPrice { get; set; }
            public int installment { get; set; }
            public string paymentChannel { get; set; }
            public string basketId { get; set; }
            public string paymentGroup { get; set; }
            public PaymentCard paymentCard { get; set; }
            public Buyer buyer { get; set; }
            public ShippingAddress shippingAddress { get; set; }
            public BillingAddress billingAddress { get; set; }
            public List<BasketItem> basketItems { get; set; }
            public string currency { get; set; }
            public string callbackUrl { get; set; }
        }

        public class ShippingAddress {
            public string address { get; set; }
            public string zipCode { get; set; }
            public string contactName { get; set; }
            public string city { get; set; }
            public string country { get; set; }
        }
    }
}

