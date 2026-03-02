using ecommerce.Cargo.Mng.Models;

namespace ecommerce.Cargo.Mng;

public partial class MngClient
{
    public async Task<CreateBarcodeResponse> CreateBarcodeAsync(CreateBarcodeRequest request)
    {
        var result = await MngRestClient.PostAsync<CreateBarcodeResponse>(
            MngClientConstants.CreateBarcodePath,
            request
        );

        return result.Data;
    }

    public async Task CancelShipmentAsync(CancelShipmentRequest request)
    {
        await MngRestClient.PutAsync<object>(
            MngClientConstants.CancelShipmentPath,
            request
        );
    }

    public async Task UpdateShipmentAsync(UpdateShipmentRequest request)
    {
        await MngRestClient.PutAsync<object>(
            MngClientConstants.UpdateShipmentPath,
            request
        );
    }
}