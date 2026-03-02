using ecommerce.Cargo.Yurtici.KOPSOrderStatusWebServices;
using ecommerce.Cargo.Yurtici.KOPSWebServices;
using ecommerce.Cargo.Yurtici.KOPSWebServices;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
namespace ecommerce.Cargo.Yurtici;

public class YurticiClient {
    private YurticiOptions Options { get; }
    private NgiShipmentInterfaceServicesClient YurticiServiceClient { get; }
    private WsReportWithReferenceServicesClient YurticiStatusServiceClient { get; set; }

    public YurticiClient(IOptions<YurticiOptions> options) {
        Options = options.Value;
        YurticiServiceClient = new NgiShipmentInterfaceServicesClient(NgiShipmentInterfaceServicesClient.EndpointConfiguration.NgiShipmentInterfaceServicesPort, Options.ServiceUrl);
        YurticiStatusServiceClient = new WsReportWithReferenceServicesClient(WsReportWithReferenceServicesClient.EndpointConfiguration.WsReportWithReferenceServicesPort, Options.StatusCheckServiceUrl);
    }

    public async Task<XShipmentDataResponse> CreateOrderAsync(createNgiShipmentWithAddress request) {
        request.wsUserLanguage = YurticiClientConstants.WebServiceLanguage;
        request.wsUserName = Options.UserName;
        request.wsPassword = Options.Password;
        var result = await YurticiServiceClient.createNgiShipmentWithAddressAsync(request);

        return result.createNgiShipmentWithAddressResponse.XShipmentDataResponse;
    }

    public async Task<XCancelShipmentResponse> CancelOrderAsync(cancelNgiShipmentWithoutReturn request) {
        request.wsUserLanguage = YurticiClientConstants.WebServiceLanguage;
        request.wsUserName = Options.UserName;
        request.wsPassword = Options.Password;

        var result = await YurticiServiceClient.cancelNgiShipmentWithoutReturnAsync(request);

        return result.cancelNgiShipmentWithoutReturnResponse.XCancelShipmentResponse;
    }

    public async Task<listInvDocumentInterfaceByReferenceResponse> GetShipmentStatusAsync(listInvDocumentInterfaceByReference request) {
        request.language = YurticiClientConstants.WebServiceLanguage;
        request.userName = Options.UserName;
        request.password = Options.Password;
        request.custParamsVO = new CustParamsVO()
        {
            invCustIdArray = new[] { YurticiClientConstants.CustId }
        };

        var result = await YurticiStatusServiceClient.listInvDocumentInterfaceByReferenceAsync(request);

        return result.listInvDocumentInterfaceByReferenceResponse;
    }

    public async Task<XCancelShipmentResponse> CreateReturnOrderAsync(cancelNgiShipment request) {
        request.wsUserLanguage = YurticiClientConstants.WebServiceLanguage;
        request.wsUserName = Options.UserName;
        request.wsPassword = Options.Password;
        //request.fieldName = Options.ReturnFieldName;

        var result = await YurticiServiceClient.cancelNgiShipmentAsync(request);

        return result.cancelNgiShipmentResponse.XCancelShipmentResponse;
    }

    //public async Task<extendedBaseResultVO> CancelReturnOrderAsync(cancelNgiShipment request)
    //{
    //    request.wsUserLanguage = YurticiClientConstants.WebServiceLanguage;
    //    request.wsUserName = Options.UserName;
    //    request.wsPassword = Options.Password;
    //    //request.fieldName = Options.ReturnFieldName;

    //    var result = await YurticiServiceClient.cancelNgiShipmentAsync(request);

    //    return result.cancelReturnShipmentCodeResponse.ExtendedBaseResultVO;
    //}
}