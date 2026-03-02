using ecommerce.Iyzico.Payment.Dtos;
using Iyzipay.Model;
using Iyzipay.Request;
namespace ecommerce.Iyzico.Payment.Interface {
    public interface IPaymentService {
        Task<CardSaveResultDto> CardSave(CreateCardRequest cardRequest);
        Task<CardUpdateresultDto> CardUpdate(CreateCardRequest cardRequest);
        Task<CardList> CardListAll(RetrieveCardListRequest retrieveCardListRequest);
        Task<CardDeleteResultDto> CardDelete(DeleteCardRequest deleteCardRequest);
        Task<InstallmentInfo> CardInstallmentList(RetrieveInstallmentInfoRequest retrieveInstallmentInfoRequest);
        Task<SubMerchant> SubMerchantCheck(RetrieveSubMerchantRequest retrieveSubMerchantRequest);
        Task<SubMerchant> CreateSubMerhant(CreateSubMerchantRequest createSubMerchantRequest);
        Task<SubMerchant> UpdateSubMerhant(UpdateSubMerchantRequest updateSubMerchantRequest);
        Task<Approval> ApprovalPaymentSubMerhant(CreateApprovalRequest createApprovalRequest);
        Task<Payment3DResponseDto> Payment3DRequest(CreatePaymentRequest paymentRequestDto);
        Task<PaymentResource> Payment3DHtmlRequest(Payment3DHtmlRequest payment3DHtmlRequest);
        Task<PaymentRefundResponse> PaymentRefund(PaymentRefundRequest paymentRefundRequest);
        Task<SubMerhantApprovePaymentResponse> SubMerhantPaymentApprove(SubmerhantApprovePaymentRequest submerhantApprove);
        Task<SubmerhantPaymentUpdateResponse> SubmerhantPaymentUpdate(UpdatePaymentItemRequest updatePaymentItemRequest);
        Task<PaymentCancelResponse> PaymentCancel(CreateCancelRequest createCancelRequest);
        Task<PaymentFormResponse> PaymentFormRequest(CreatePayWithIyzicoInitializeRequest createCheckout);
        Task<PaymentFormResponse> PaymentFormCreate(CreateCheckoutFormInitializeRequest model);


    }
}

