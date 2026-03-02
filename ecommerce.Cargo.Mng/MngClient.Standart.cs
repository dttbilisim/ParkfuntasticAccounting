using ecommerce.Cargo.Mng.Models;

namespace ecommerce.Cargo.Mng;

public partial class MngClient
{
    public async Task<CreateOrderResponse> CreateOrderAsync(CreateDetailOrder request)
    {
        var result = await MngRestClient.PostAsync<CreateOrderResponse>(
            MngClientConstants.CreateOrderPath,
            request
        );

        return result.Data!;
    }

    public async Task<CreateOrderResponse> CreateReturnOrderAsync(CreateReturnOrderRequest request)
    {
        var result = await MngRestClient.PostAsync<CreateOrderResponse>(
            MngClientConstants.CreateReturnOrderPath,
            request
        );

        return result.Data!;
    }

    public async Task CancelOrderAsync(CancelCargoRequest request)
    {
        var result = await MngRestClient.PostAsync<CreateOrderResponse>(
            MngClientConstants.CancelOrderPath,
            request
        );

       


    }

    public async Task<CalculateResponse> CalculateAsync(CalculateRequest request)
    {
        var result = await MngRestClient.PostAsync<CalculateResponse>(
            MngClientConstants.CalculatePath,
            request
        );

        return result.Data;
    }

    public async Task<List<GetShipmentStatusResponse>> GetShipmentStatus(IEnumerable<string> orderIds)
    {
        var result = await MngRestClient.GetAsync<List<GetShipmentStatusResponse>>(
            MngClientConstants.GetShipmentStatusPath,
            urlSegments: new Dictionary<string, string>
            {
                { "orderId", string.Join(",", orderIds) }
            }
        );

        return result.Data;
    }

    public async Task<List<GetShipmentStatusResponse>> GetShipmentStatusByShipmentId(IEnumerable<string> shipmentIds)
    {
        var result = await MngRestClient.GetAsync<List<GetShipmentStatusResponse>>(
            MngClientConstants.GetShipmentStatusByShipmentIdPath,
            urlSegments: new Dictionary<string, string>
            {
                { "shipmentId", string.Join(",", shipmentIds) }
            }
        );

        return result.Data;
    }
}