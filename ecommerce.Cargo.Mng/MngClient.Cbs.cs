using ecommerce.Cargo.Mng.Models;

namespace ecommerce.Cargo.Mng;

public partial class MngClient
{
    public async Task<List<CodeName>> GetCitiesAsync()
    {
        var result = await MngRestClient.GetAsync<List<CodeName>>(
            MngClientConstants.GetCitiesPath
        );

        return result.Data;
    }

    public async Task<List<CodeName>> GetCitiesAsync(string cityCode)
    {
        var result = await MngRestClient.GetAsync<List<CodeName>>(
            MngClientConstants.GetDistrictsPath,
            urlSegments: new Dictionary<string, string>
            {
                { "cityCode", cityCode }
            }
        );

        return result.Data;
    }

    public async Task<List<GetNeighborhoodResponse>> GetNeighborhoodsAsync(string cityCode, string districtCode)
    {
        var result = await MngRestClient.GetAsync<List<GetNeighborhoodResponse>>(
            MngClientConstants.GetNeighborhoodsPath,
            urlSegments: new Dictionary<string, string>
            {
                { "cityCode", cityCode },
                { "districtCode", districtCode }
            }
        );

        return result.Data;
    }
}