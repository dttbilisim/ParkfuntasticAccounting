using ecommerce.Virtual.Pos.Requests;
using ecommerce.Virtual.Pos.Results;
using Microsoft.AspNetCore.Http;
namespace ecommerce.Virtual.Pos.Abstract;
public interface IPaymentProvider{
    Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request);
    Task<VerifyGatewayResult> VerifyGateway(VerifyGatewayRequest request, PaymentGatewayRequest gatewayRequest, IFormCollection form);
    Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request);
    Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request);
    Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request);
    Dictionary<string, string> TestParameters { get; }
}
