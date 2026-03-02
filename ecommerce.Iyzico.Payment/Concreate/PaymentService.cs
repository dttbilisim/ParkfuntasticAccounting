using System.Globalization;
using ecommerce.Iyzico.Payment.Dtos;
using ecommerce.Iyzico.Payment.Interface;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Configuration;
namespace ecommerce.Iyzico.Payment.Concreate{
    public class PaymentService : IPaymentService{
        private readonly IConfiguration _configuration;
        private readonly Options options = new Options();
        public PaymentService(IConfiguration configuration){
            _configuration = configuration;
            var config = _configuration.GetSection("Iyzico");
            options.ApiKey = config["ApiKey"];
            options.SecretKey = config["SecretKey"];
            options.BaseUrl = config["BaseUrl"];
        }
        public Task<CardList> CardListAll(RetrieveCardListRequest retrieveCardListRequest){
            var request = new RetrieveCardListRequest{Locale = Locale.TR.ToString(), ConversationId = retrieveCardListRequest.ConversationId, CardUserKey = retrieveCardListRequest.CardUserKey};
            var cardList = CardList.Retrieve(request, options);
            return Task.FromResult(cardList);
        }
        public Task<CardSaveResultDto> CardSave(CreateCardRequest cardRequest){
            var request = new CreateCardRequest{Locale = Locale.TR.ToString(), ConversationId = cardRequest.ConversationId, Email = cardRequest.Email, ExternalId = cardRequest.ExternalId};
            var cardInformation = new CardInformation{
                CardAlias = cardRequest.Card.CardAlias,
                CardHolderName = cardRequest.Card.CardHolderName,
                CardNumber = cardRequest.Card.CardNumber,
                ExpireMonth = cardRequest.Card.ExpireMonth,
                ExpireYear = cardRequest.Card.ExpireYear
            };
            request.Card = cardInformation;
            var card = Card.Create(request, options);
            return Task.FromResult(new CardSaveResultDto(){
                    externalId = card.ExternalId,
                    email = card.Email,
                    cardUserKey = card.CardUserKey,
                    cardToken = card.CardToken,
                    cardAlias = card.CardAlias,
                    cardBankCode = (long) card.CardBankCode,
                    cardBankName = card.CardBankName,
                    conversationId = card.ConversationId,
                    systemTime = card.SystemTime,
                    status = card.Status
                }
            );
        }
        public Task<CardUpdateresultDto> CardUpdate(CreateCardRequest cardRequest){
            var request = new CreateCardRequest{Locale = Locale.TR.ToString(), ConversationId = cardRequest.ConversationId, CardUserKey = cardRequest.CardUserKey};
            var cardInformation = new CardInformation{
                CardAlias = cardRequest.Card.CardAlias,
                CardHolderName = cardRequest.Card.CardHolderName,
                CardNumber = cardRequest.Card.CardNumber,
                ExpireMonth = cardRequest.Card.ExpireMonth,
                ExpireYear = cardRequest.Card.ExpireYear
            };
            request.Card = cardInformation;
            var card = Card.Create(request, options);
            return Task.FromResult(new CardUpdateresultDto{
                    binNumber = card.BinNumber,
                    cardAlias = card.CardAlias,
                    cardAssociation = card.CardAssociation,
                    cardBankCode = (long) card.CardBankCode,
                    cardBankName = card.CardBankName,
                    cardFamily = card.CardFamily,
                    cardToken = card.CardToken,
                    cardType = card.CardType,
                    cardUserKey = card.CardUserKey,
                    conversationId = card.ConversationId,
                    locale = card.Locale,
                    status = card.Status,
                    systemTime = card.SystemTime
                }
            );
        }
        public Task<CardDeleteResultDto> CardDelete(DeleteCardRequest deleteCardRequest){
            var request = new DeleteCardRequest{Locale = Locale.TR.ToString(), ConversationId = deleteCardRequest.ConversationId, CardToken = deleteCardRequest.CardToken, CardUserKey = deleteCardRequest.CardUserKey};
            var card = Card.Delete(request, options);
            return Task.FromResult(new CardDeleteResultDto{conversationId = card.ConversationId, status = card.Status, locale = card.Locale, systemTime = card.SystemTime});
        }
        public Task<InstallmentInfo> CardInstallmentList(RetrieveInstallmentInfoRequest retrieveInstallmentInfoRequest){
            var request = new RetrieveInstallmentInfoRequest{Locale = Locale.TR.ToString(), ConversationId = retrieveInstallmentInfoRequest.ConversationId, BinNumber = retrieveInstallmentInfoRequest.BinNumber, Price = retrieveInstallmentInfoRequest.Price};
            var installmentInfo = InstallmentInfo.Retrieve(request, options);
            return Task.FromResult(installmentInfo);
        }
        public Task<SubMerchant> CreateSubMerhant(CreateSubMerchantRequest createSubMerchantRequest){
            var request = new CreateSubMerchantRequest{
                Locale = Locale.TR.ToString(),
                ConversationId = createSubMerchantRequest.ConversationId,
                SubMerchantExternalId = createSubMerchantRequest.SubMerchantExternalId,
                SubMerchantType = SubMerchantType.PRIVATE_COMPANY.ToString(),
                Address = createSubMerchantRequest.Address,
                TaxOffice = createSubMerchantRequest.TaxOffice,
                LegalCompanyTitle = createSubMerchantRequest.LegalCompanyTitle,
                Email = createSubMerchantRequest.Email,
                GsmNumber = createSubMerchantRequest.GsmNumber,
                Name = createSubMerchantRequest.Name,
                Iban = createSubMerchantRequest.Iban,
                IdentityNumber = createSubMerchantRequest.IdentityNumber,
                Currency = Currency.TRY.ToString()
            };
            var subMerchant = SubMerchant.Create(request, options);
            return Task.FromResult(subMerchant);
        }
        public Task<SubMerchant> UpdateSubMerhant(UpdateSubMerchantRequest updateSubMerchantRequest){
            var request = new UpdateSubMerchantRequest{
                Locale = Locale.TR.ToString(),
                ConversationId = updateSubMerchantRequest.ConversationId,
                ContactName = updateSubMerchantRequest.ContactName,
                Currency = Currency.TRY.ToString(),
                IdentityNumber = updateSubMerchantRequest.TaxNumber,
                TaxOffice = updateSubMerchantRequest.TaxOffice,
                LegalCompanyTitle = updateSubMerchantRequest.LegalCompanyTitle,
                Email = updateSubMerchantRequest.Email,
                GsmNumber = updateSubMerchantRequest.GsmNumber,
                Name = updateSubMerchantRequest.Name,
                Iban = updateSubMerchantRequest.Iban,
                ContactSurname = updateSubMerchantRequest.ContactSurname,
                TaxNumber = updateSubMerchantRequest.TaxNumber,
                Address = updateSubMerchantRequest.Address,
                SubMerchantKey = updateSubMerchantRequest.SubMerchantKey,
                
            };
            var approval = SubMerchant.Update(request, options);
            return Task.FromResult(approval);
        }
        public Task<Approval> ApprovalPaymentSubMerhant(CreateApprovalRequest createApprovalRequest){
            var request = new CreateApprovalRequest{Locale = Locale.TR.ToString(), ConversationId = createApprovalRequest.ConversationId, PaymentTransactionId = createApprovalRequest.PaymentTransactionId};
            var approval = Approval.Create(request, options);
            return Task.FromResult(approval);
        }
        public Task<SubMerchant> SubMerchantCheck(RetrieveSubMerchantRequest retrieveSubMerchantRequest){
            var request = new RetrieveSubMerchantRequest{Locale = Locale.TR.ToString(), ConversationId = retrieveSubMerchantRequest.ConversationId, SubMerchantExternalId = retrieveSubMerchantRequest.SubMerchantExternalId};
            var subMerchant = SubMerchant.Retrieve(request, options);
            return Task.FromResult(subMerchant);
        }
        public Task<Payment3DResponseDto> Payment3DRequest(CreatePaymentRequest paymentRequestDto){
            // CheckoutFormInitialize forminitalize = CheckoutFormInitialize.Create(paymentRequestDto, options);
            var threedsInitialize = ThreedsInitialize.Create(paymentRequestDto, options);
            // Iyzipay.Model.Payment Pa = Iyzipay.Model.Payment.Payment.Create(request, options);
            return Task.FromResult(new Payment3DResponseDto{
                    conversationId = threedsInitialize.ConversationId,
                    threeDSHtmlContent = threedsInitialize.HtmlContent,
                    status = threedsInitialize.Status,
                    locale = threedsInitialize.Locale,
                    systemTime = threedsInitialize.SystemTime,
                    ErrorCode = threedsInitialize.ErrorCode,
                    ErrorMessage = threedsInitialize.ErrorMessage
                }
            );
        }
        public Task<PaymentResource> Payment3DHtmlRequest(Payment3DHtmlRequest payment3DHtmlRequest){
            var payment3DHtmlResponse = new PaymentResource();
            var request = new RetrievePayWithIyzicoRequest{Locale = Locale.TR.ToString(), ConversationId = payment3DHtmlRequest.conversationId, Token = payment3DHtmlRequest.token};
            var threedsPayment = PayWithIyzico.Retrieve(request, options);
            var transactionsList = new List<PaymentItem>();
            foreach(var paymenttrans in threedsPayment.PaymentItems){
                var itemTransactions = new PaymentItem{
                    ItemId = paymenttrans.ItemId,
                    PaymentTransactionId = paymenttrans.PaymentTransactionId,
                    TransactionStatus = (int) paymenttrans.TransactionStatus,
                    Price = paymenttrans.Price,
                    PaidPrice = paymenttrans.PaidPrice,
                    SubMerchantPrice = paymenttrans.SubMerchantPrice,
                    SubMerchantPayoutAmount = paymenttrans.SubMerchantPayoutAmount,
                    MerchantPayoutAmount = paymenttrans.MerchantPayoutAmount
                };
                itemTransactions.TransactionStatus = (int) paymenttrans.TransactionStatus;
                transactionsList.Add(itemTransactions);
            }
            if(threedsPayment.Status.ToLower() == "success"){
                {
                    payment3DHtmlResponse.ConversationId = threedsPayment.ConversationId;
                    payment3DHtmlResponse.BasketId = threedsPayment.BasketId;
                    payment3DHtmlResponse.BinNumber = threedsPayment.BinNumber;
                    payment3DHtmlResponse.CardAssociation = threedsPayment.CardAssociation;
                    payment3DHtmlResponse.CardFamily = threedsPayment.CardFamily;
                    payment3DHtmlResponse.CardType = threedsPayment.CardType;
                    payment3DHtmlResponse.Currency = threedsPayment.Currency;
                    payment3DHtmlResponse.FraudStatus = (int) threedsPayment.FraudStatus;
                    payment3DHtmlResponse.Installment = (int) threedsPayment.Installment;
                    payment3DHtmlResponse.IyziCommissionFee = threedsPayment.IyziCommissionFee;
                    payment3DHtmlResponse.IyziCommissionRateAmount = threedsPayment.IyziCommissionRateAmount;
                    payment3DHtmlResponse.Locale = threedsPayment.Locale;
                    payment3DHtmlRequest.token = threedsPayment.Token;
                    payment3DHtmlResponse.PaidPrice = threedsPayment.PaidPrice.ToString(CultureInfo.InvariantCulture);
                    payment3DHtmlResponse.PaymentId = threedsPayment.PaymentId;
                    payment3DHtmlResponse.Price = threedsPayment.Price.ToString(CultureInfo.InvariantCulture);
                    payment3DHtmlResponse.Status = threedsPayment.PaymentStatus == "SUCCESS" ? "success" : "failure";
                    payment3DHtmlResponse.SystemTime = threedsPayment.SystemTime;
                    payment3DHtmlResponse.PaymentItems = transactionsList;
                }
            }
            return Task.FromResult(payment3DHtmlResponse);
        }
        public Task<PaymentRefundResponse> PaymentRefund(PaymentRefundRequest paymentRefundRequest){
            var request = new CreateRefundRequest{
                ConversationId = paymentRefundRequest.conversationId,
                Locale = Locale.TR.ToString(),
                PaymentTransactionId = paymentRefundRequest.paymentTransactionId,
                Price = paymentRefundRequest.price.ToString(CultureInfo.InvariantCulture),
                Ip = "85.34.78.112",
                Currency = Currency.TRY.ToString()
            };
            var refund = Refund.Create(request, options);
            return Task.FromResult(new PaymentRefundResponse{
                    conversationId = refund.ConversationId,
                    currency = refund.Currency,
                    locale = refund.Locale,
                    paymentId = refund.PaymentId,
                    paymentTransactionId = refund.PaymentTransactionId,
                    price = Convert.ToDecimal(refund.Price),
                    status = refund.Status,
                    systemTime = refund.SystemTime
                }
            );
        }
        public Task<SubMerhantApprovePaymentResponse> SubMerhantPaymentApprove(SubmerhantApprovePaymentRequest submerhantApprove){
            var request = new CreateApprovalRequest{Locale = Locale.TR.ToString(), PaymentTransactionId = submerhantApprove.paymentTransactionId};
            var approval = Approval.Create(request, options);
            return Task.FromResult(new SubMerhantApprovePaymentResponse{status = approval.Status, paymentTransactionId = approval.PaymentTransactionId, errorCode = approval.ErrorCode, errorMessage = approval.ErrorMessage});
        }
        public Task<SubmerhantPaymentUpdateResponse> SubmerhantPaymentUpdate(UpdatePaymentItemRequest updatePaymentItemRequest){
            var paymentItem = PaymentItem.Update(updatePaymentItemRequest, options);
            return Task.FromResult(new SubmerhantPaymentUpdateResponse{status = paymentItem.Status, errorCode = paymentItem.ErrorCode, errorMessage = paymentItem.ErrorMessage});
        }
        public Task<PaymentCancelResponse> PaymentCancel(CreateCancelRequest createCancelRequest){
            var cancel = Cancel.Create(createCancelRequest, options);
            return Task.FromResult(new PaymentCancelResponse{ConversationId = cancel.ConversationId, ErrorCode = cancel.ErrorCode, Status = cancel.Status, ErrorMessage = cancel.ErrorMessage});
        }
        public Task<PaymentFormResponse> PaymentFormRequest(CreatePayWithIyzicoInitializeRequest createCheckout){
            var checkoutFormInitialize = PayWithIyzicoInitialize.Create(createCheckout, options);
            return Task.FromResult(new PaymentFormResponse(){
                    checkoutFormContent = checkoutFormInitialize.CheckoutFormContent,
                    paymentPageUrl = checkoutFormInitialize.PayWithIyzicoPageUrl,
                    status = checkoutFormInitialize.Status,
                    token = checkoutFormInitialize.Token,
                    tokenExpireTime = checkoutFormInitialize.TokenExpireTime,
                    errorcode = checkoutFormInitialize.ErrorCode,
                    errormessage = checkoutFormInitialize.ErrorMessage
                }
            );
        }
        public Task<PaymentFormResponse> PaymentFormCreate(CreateCheckoutFormInitializeRequest model){
            var checkoutFormInitialize = CheckoutFormInitialize.Create(model, options);
            return Task.FromResult(new PaymentFormResponse(){
                    checkoutFormContent = checkoutFormInitialize.CheckoutFormContent,
                    errorcode = checkoutFormInitialize.ErrorMessage,
                    status = checkoutFormInitialize.Status,
                    token = checkoutFormInitialize.Token,
                    tokenExpireTime = checkoutFormInitialize.TokenExpireTime,
                    errormessage = checkoutFormInitialize.ErrorMessage,
                    paymentPageUrl = checkoutFormInitialize.PaymentPageUrl
                }
            );
        }
    }
}
