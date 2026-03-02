using ecommerce.Cargo.Sendeo.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using RestSharp;
namespace ecommerce.Cargo.Sendeo{
    public class SendeoClient{
        private SendeoOptions Options{get;}
        private IMemoryCache Cache{get;}
        private SendeoRestClient SendeoRestClient{get;}
        public SendeoClient(IOptions<SendeoOptions> options, IMemoryCache cache){
            Options = options.Value;
            Cache = cache;
            SendeoRestClient = new SendeoRestClient{LoginDelegate = LoginAsync, LogoutDelegate = LogoutAsync};
        }
        public async Task<City ?> GetCityWithDistrictsAsync(string cityName, string ? districtName = null){
            return await Cache.GetOrCreateAsync(string.Format(SendeoClientConstants.CityCacheKey, cityName), async entry => {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(SendeoClientConstants.CityCacheTimeInMinutes);
                    var result = await SendeoRestClient.GetAsync<ResponseBase<City>>(SendeoClientConstants.CargoGetCityDistrictsPath, new{CityName = cityName, DistrictName = districtName});
                    return result.Data.Result;
                }
            );
        }
        public async Task<SetDeliveryResult> SetDeliveryAsync(SetDeliveryRequest request){
            var response = await SendeoRestClient.PostAsync<ResponseBase<SetDeliveryResult>>(SendeoClientConstants.CargoSetDeliveryPath, request);
            return response.Data.Result;
        }
        public async Task<bool> CancelDeliveryAsync(long trackingNo, string referenceNo){
            var response = await SendeoRestClient.PostAsync<ResponseBase<CancelDeliveryResult>>(SendeoClientConstants.CargoCancelDeliveryPath, new{}, options:new{trackingNo, referenceNo});
            return response.Data.Result.IsSuccess;
        }
        public async Task<Delivery> GetDeliveryAsync(string trackingNo){
            var result = await SendeoRestClient.GetAsync<ResponseBase<Delivery>>(SendeoClientConstants.CargoTrackDeliveryPath, new{trackingNo});
            return result.Data.Result;
        }
        public async Task<CargoListResult> GetCargoListAsync(CargoListRequest request){
            var result = await SendeoRestClient.PostAsync<ResponseBase<CargoListResult>>(SendeoClientConstants.CargoListPath, request);
            return result.Data.Result;
        }
        private async Task LoginAsync(RestRequest request){
            if(request.Resource == SendeoClientConstants.TokenPath){
                return;
            }
            var cacheKey = string.Format(SendeoClientConstants.AuthCacheKey, Options.CustomerName);
            var cacheResult = await Cache.GetOrCreateAsync<SendeoSessionCacheItem>(cacheKey, async entry => {
                    var tokenResponse = await SendeoRestClient.PostAsync<ResponseBase<LoginResult>>(SendeoClientConstants.TokenPath, new LoginRequest{CustomerName = Options.CustomerName, Password = Options.Password}, authenticate:false);
                    
                    if (tokenResponse?.Data?.Result == null)
                    {
                        var errorMsg = "Sendeo Login API response is invalid.";
                        if (tokenResponse == null) errorMsg += " Response is null.";
                        else if (tokenResponse.Data == null) errorMsg += " Response.Data is null.";
                        else 
                        {
                            errorMsg += " Response.Data.Result is null.";
                            if (!string.IsNullOrEmpty(tokenResponse.Data.ExceptionMessage)) 
                                errorMsg += $" Api Msg: {tokenResponse.Data.ExceptionMessage}";
                            if (!string.IsNullOrEmpty(tokenResponse.Data.ExceptionDescription)) 
                                errorMsg += $" Api Desc: {tokenResponse.Data.ExceptionDescription}";
                            
                            errorMsg += $" Status: {tokenResponse.Data.StatusCode}";
                        }
                        
                        throw new Exception(errorMsg);
                    }

                    var cacheItem = new SendeoSessionCacheItem{CustomerId = tokenResponse.Data.Result.CustomerId, CustomerTitle = tokenResponse.Data.Result.CustomerTitle, Token = tokenResponse.Data.Result.Token,};
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(SendeoClientConstants.TokenExpireInMinutes - 5);
                    return cacheItem;
                }
            );
            if(string.IsNullOrEmpty(cacheResult?.Token)){
                return;
            }
            request.AddOrUpdateHeader(HeaderNames.Authorization, $"Bearer {cacheResult.Token}");
        }
        private Task LogoutAsync(){
            Cache.Remove(string.Format(SendeoClientConstants.AuthCacheKey, Options.CustomerName));
            return Task.CompletedTask;
        }
    }
}
