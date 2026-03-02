using System.Text.Json;
using ecommerce.Web.Domain.Dtos.Address;
using ecommerce.Web.Domain.Dtos.Options;
using Microsoft.Extensions.Options;

namespace ecommerce.Web.Domain.Services
{
    public class AddressService : IAddressService
    {
        private readonly HttpClient _httpClient;
        private readonly AddressApiOptions _options;

        public AddressService(HttpClient httpClient, IOptions<AddressApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<List<CityDto>> GetCitiesAsync()
        {
            try
            {
                Console.WriteLine($"AddressService: Getting cities from {_options.BaseUrl}/city");
                Console.WriteLine($"AddressService: ApiCode = {_options.ApiCode}");
                var result = await PostAsync<List<CityDto>>("/city");
                Console.WriteLine($"AddressService: Received {result?.Count ?? 0} cities");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCitiesAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<CityDto>();
            }
        }

        public async Task<List<TownDto>> GetTownsAsync(int cityId)
        {
            try
            {
                return await PostFormAsync<List<TownDto>>("/town", ("city_id", cityId.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTownsAsync for cityId {cityId}: {ex.Message}");
                return new List<TownDto>();
            }
        }

        public async Task<List<NeighboorDto>> GetNeighboorsAsync(int townId)
        {
            try
            {
                return await PostFormAsync<List<NeighboorDto>>("/neighboor", ("town_id", townId.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetNeighboorsAsync for townId {townId}: {ex.Message}");
                return new List<NeighboorDto>();
            }
        }

        public async Task<List<StreetDto>> GetStreetsAsync(int neighboorId)
        {
            try
            {
                return await PostFormAsync<List<StreetDto>>("/street", ("neighboor_id", neighboorId.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStreetsAsync for neighboorId {neighboorId}: {ex.Message}");
                return new List<StreetDto>();
            }
        }

        public async Task<List<BuildingDto>> GetBuildingsAsync(int streetId)
        {
            try
            {
                return await PostFormAsync<List<BuildingDto>>("/building", ("street_id", streetId.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBuildingsAsync for streetId {streetId}: {ex.Message}");
                return new List<BuildingDto>();
            }
        }

        public async Task<List<HomeDto>> GetHomesAsync(int buildingId)
        {
            try
            {
                return await PostFormAsync<List<HomeDto>>("/home", ("building_id", buildingId.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetHomesAsync for buildingId {buildingId}: {ex.Message}");
                return new List<HomeDto>();
            }
        }

        public async Task<List<AddressInfDto>> GetAddressInfoAsync(int homeId)
        {
            try
            {
                // Some providers expect form body instead of query string
                return await PostFormAsync<List<AddressInfDto>>("/address_inf", ("home_id", homeId.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAddressInfoAsync for homeId {homeId}: {ex.Message}");
                return new List<AddressInfDto>();
            }
        }

        public async Task<string?> GetAddressDetailAsync(int homeId)
        {
            try
            {
                var form = new MultipartFormDataContent { { new StringContent(homeId.ToString()), "home_id" } };
                var response = await _httpClient.PostAsync($"{_options.BaseUrl}/address_inf", form);
                response.EnsureSuccessStatusCode();
                var raw = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"RAW JSON from /address_inf: {raw}");

                // Endpoint returns { status: true, data: { address: "..." } }
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    if (data.TryGetProperty("address", out var addrProp))
                    {
                        return addrProp.GetString();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAddressDetailAsync for homeId {homeId}: {ex.Message}");
                return null;
            }
        }

        // === Helpers ===
        private async Task<T> PostAsync<T>(string endpoint)
        {
            var response = await _httpClient.PostAsync($"{_options.BaseUrl}{endpoint}", null);
            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"RAW JSON from {endpoint}: {raw}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var wrapper = JsonSerializer.Deserialize<ApiResponse<T>>(raw, options);
            Console.WriteLine($"Deserialized wrapper: Status={wrapper?.Status}, Data={wrapper.Data != null}");
            
            if (wrapper == null || wrapper.Data == null)
            {
                Console.WriteLine("Wrapper or Data is null");
                throw new Exception("Null or invalid response");
            }

            return wrapper.Data;
        }

        private async Task<T> PostFormAsync<T>(string endpoint, params (string Key, string Value)[] formParams)
        {
            var form = new MultipartFormDataContent();
            foreach (var (k, v) in formParams) form.Add(new StringContent(v), k);

            var response = await _httpClient.PostAsync($"{_options.BaseUrl}{endpoint}", form);
            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"RAW JSON from {endpoint}: {raw}");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var wrapper = JsonSerializer.Deserialize<ApiResponse<T>>(raw, options);
            if (wrapper == null || wrapper.Data == null)
                throw new Exception("Null or invalid response");

            return wrapper.Data;
        }
    }
}
