using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Telecom.Address.Abstract;
using Telecom.Address.Dtos;
using Telecom.Address.Options;
namespace Telecom.Address.Concreate;
public class AddressApiService : IAddressApiService
{
    private readonly HttpClient _httpClient;
    private readonly AddressApiOptions _options;

    public AddressApiService(HttpClient httpClient, IOptions<AddressApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<CityDto>> GetCitiesAsync()
    {
        try
        {
            return await PostAsync<List<CityDto>>("/city");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetCitiesAsync: {ex.Message}");
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
            // Return empty list if API fails
            return new List<HomeDto>();
        }
    }

    public async Task<List<AddressInfDto>> GetAddressInfoAsync(int homeId)
    {
        try
        {
            return await PostAsync<List<AddressInfDto>>($"/address_inf?home_id={homeId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAddressInfoAsync for homeId {homeId}: {ex.Message}");
            // Return empty list if API fails
            return new List<AddressInfDto>();
        }
    }

    public async Task<InternetInfrastructureDto> GetMemberInternetInfrastructureAsync()
        => await PostAsync<InternetInfrastructureDto>($"/internet_infrastructure/member_internet_infrastructure/{_options.ApiCode}");

    public async Task<InternetInfrastructureDto> GetTtVaeQueryAsync()
        => await PostAsync<InternetInfrastructureDto>($"/internet_infrastructure/tt_vae_query/{_options.ApiCode}");

    // === Helpers ===
    private async Task<T> PostAsync<T>(string endpoint)
    {
        var response = await _httpClient.PostAsync($"{_options.BaseUrl}{endpoint}", null);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"RAW JSON from {endpoint}: {raw}");

        var wrapper = JsonSerializer.Deserialize<ApiResponse<T>>(raw);
        if (wrapper == null || wrapper.Data == null)
            throw new Exception("Null or invalid response");

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

        var wrapper = JsonSerializer.Deserialize<ApiResponse<T>>(raw);
        if (wrapper == null || wrapper.Data == null)
            throw new Exception("Null or invalid response");

        return wrapper.Data;
    }
}