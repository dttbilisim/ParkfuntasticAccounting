using Telecom.Address.Dtos;
namespace Telecom.Address.Abstract;
public interface IAddressApiService{
    Task<List<CityDto>> GetCitiesAsync();
    Task<List<TownDto>> GetTownsAsync(int cityId);
    Task<List<NeighboorDto>> GetNeighboorsAsync(int townId);
    Task<List<StreetDto>> GetStreetsAsync(int neighboorId);
    Task<List<BuildingDto>> GetBuildingsAsync(int streetId);
    Task<List<HomeDto>> GetHomesAsync(int buildingId);
    Task<List<AddressInfDto>> GetAddressInfoAsync(int homeId);

    Task<InternetInfrastructureDto> GetMemberInternetInfrastructureAsync();
    Task<InternetInfrastructureDto> GetTtVaeQueryAsync();
}
