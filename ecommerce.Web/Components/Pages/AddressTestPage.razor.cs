using ecommerce.Web.Domain.Services;
using ecommerce.Web.Domain.Dtos.Address;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace ecommerce.Web.Components.Pages
{
    public partial class AddressTestPage
    {
        [Inject] private IAddressService AddressService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private AppStateManager AppStateManager { get; set; }
        [Inject] private II18N lang { get; set; }

        private List<CityDto>? cities;
        private List<TownDto>? towns;
        private List<NeighboorDto>? neighboors;
        private List<StreetDto>? streets;
        private List<BuildingDto>? buildings;
        private List<HomeDto>? homes;

        private string selectedCityCode = "";
        private string selectedTownCode = "";
        private string selectedNeighboorCode = "";
        private string selectedStreetCode = "";
        private string selectedBuildingCode = "";
        private string selectedHomeCode = "";

        private bool isLoading = false;
        private string errorMessage = "";

        private SelectedAddress? selectedAddress;

        private Task SetLoadingAsync(bool value)
        {
            return InvokeAsync(() =>
            {
                isLoading = value;
                StateHasChanged();
            });
        }

        private async Task ExecuteLoadingAsync(Func<Task> operation, string loadingMessage, string errorPrefix)
        {
            await AppStateManager.ExecuteWithLoading(async () =>
            {
                await SetLoadingAsync(true);
                errorMessage = "";
                try
                {
                    await operation();
                }
                catch (Exception ex)
                {
                    errorMessage = $"{errorPrefix}: {ex.Message}";
                    Console.WriteLine($"{errorPrefix}: {ex.Message}");
                }
                finally
                {
                    await SetLoadingAsync(false);
                }
            }, loadingMessage);
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadCities();
        }

        private async Task LoadCities()
        {
            await ExecuteLoadingAsync(async () =>
            {
                cities = await AddressService.GetCitiesAsync();
                if (cities == null || !cities.Any())
                {
                    errorMessage = lang["AddressTest.Errors.CityEmpty"];
                }
            }, lang["AddressTest.Loading.Cities"], lang["AddressTest.Errors.CityLoad"]);
        }

        private async Task OnCityChanged(ChangeEventArgs e)
        {
            selectedCityCode = e.Value?.ToString() ?? "";
            
            // Reset all dependent selections
            selectedTownCode = "";
            selectedNeighboorCode = "";
            selectedStreetCode = "";
            selectedBuildingCode = "";
            selectedHomeCode = "";
            
            towns = null;
            neighboors = null;
            streets = null;
            buildings = null;
            homes = null;
            selectedAddress = null;

            if (string.IsNullOrEmpty(selectedCityCode))
            {
                StateHasChanged();
                return;
            }

            await ExecuteLoadingAsync(async () =>
            {
                var cityId = int.Parse(selectedCityCode);
                towns = await AddressService.GetTownsAsync(cityId);

                if (towns == null || !towns.Any())
                {
                    errorMessage = lang["AddressTest.Errors.TownEmpty"];
                }
            }, lang["AddressTest.Loading.Towns"], lang["AddressTest.Errors.TownLoad"]);
        }

        private async Task OnTownChanged(ChangeEventArgs e)
        {
            selectedTownCode = e.Value?.ToString() ?? "";
            
            // Reset dependent selections
            selectedNeighboorCode = "";
            selectedStreetCode = "";
            selectedBuildingCode = "";
            selectedHomeCode = "";
            
            neighboors = null;
            streets = null;
            buildings = null;
            homes = null;
            selectedAddress = null;

            if (string.IsNullOrEmpty(selectedTownCode))
            {
                StateHasChanged();
                return;
            }

            await ExecuteLoadingAsync(async () =>
            {
                var townId = int.Parse(selectedTownCode);
                neighboors = await AddressService.GetNeighboorsAsync(townId);

                if (neighboors == null || !neighboors.Any())
                {
                    errorMessage = lang["AddressTest.Errors.NeighboorEmpty"];
                }
            }, lang["AddressTest.Loading.Neighboors"], lang["AddressTest.Errors.NeighboorLoad"]);
        }

        private async Task OnNeighboorChanged(ChangeEventArgs e)
        {
            selectedNeighboorCode = e.Value?.ToString() ?? "";
            
            // Reset dependent selections
            selectedStreetCode = "";
            selectedBuildingCode = "";
            selectedHomeCode = "";
            
            streets = null;
            buildings = null;
            homes = null;
            selectedAddress = null;

            if (string.IsNullOrEmpty(selectedNeighboorCode))
            {
                StateHasChanged();
                return;
            }

            await ExecuteLoadingAsync(async () =>
            {
                var neighboorId = int.Parse(selectedNeighboorCode);
                streets = await AddressService.GetStreetsAsync(neighboorId);

                if (streets == null || !streets.Any())
                {
                    errorMessage = lang["AddressTest.Errors.StreetEmpty"];
                }
            }, lang["AddressTest.Loading.Streets"], lang["AddressTest.Errors.StreetLoad"]);
        }

        private async Task OnStreetChanged(ChangeEventArgs e)
        {
            selectedStreetCode = e.Value?.ToString() ?? "";
            
            // Reset dependent selections
            selectedBuildingCode = "";
            selectedHomeCode = "";
            
            buildings = null;
            homes = null;
            selectedAddress = null;

            if (string.IsNullOrEmpty(selectedStreetCode))
            {
                StateHasChanged();
                return;
            }

            await ExecuteLoadingAsync(async () =>
            {
                var streetId = int.Parse(selectedStreetCode);
                buildings = await AddressService.GetBuildingsAsync(streetId);

                if (buildings == null || !buildings.Any())
                {
                    errorMessage = lang["AddressTest.Errors.BuildingEmpty"];
                }
            }, lang["AddressTest.Loading.Buildings"], lang["AddressTest.Errors.BuildingLoad"]);
        }

        private async Task OnBuildingChanged(ChangeEventArgs e)
        {
            selectedBuildingCode = e.Value?.ToString() ?? "";
            
            // Reset dependent selections
            selectedHomeCode = "";
            
            homes = null;
            selectedAddress = null;

            if (string.IsNullOrEmpty(selectedBuildingCode))
            {
                StateHasChanged();
                return;
            }

            await ExecuteLoadingAsync(async () =>
            {
                var buildingId = int.Parse(selectedBuildingCode);
                homes = await AddressService.GetHomesAsync(buildingId);

                if (homes == null || !homes.Any())
                {
                    errorMessage = lang["AddressTest.Errors.HomeEmpty"];
                }
            }, lang["AddressTest.Loading.Homes"], lang["AddressTest.Errors.HomeLoad"]);
        }

        private async Task OnHomeChanged(ChangeEventArgs e)
        {
            selectedHomeCode = e.Value?.ToString() ?? "";
            selectedAddress = null;

            if (string.IsNullOrEmpty(selectedHomeCode))
            {
                StateHasChanged();
                return;
            }

            // Create selected address object
            selectedAddress = new SelectedAddress
            {
                CityName = cities?.FirstOrDefault(c => c.Code == selectedCityCode)?.Name ?? "",
                TownName = towns?.FirstOrDefault(t => t.Code == selectedTownCode)?.Name ?? "",
                NeighboorName = neighboors?.FirstOrDefault(n => n.Code == selectedNeighboorCode)?.Name ?? "",
                StreetName = streets?.FirstOrDefault(s => s.Code == selectedStreetCode)?.Name ?? "",
                BuildingName = buildings?.FirstOrDefault(b => b.Code == selectedBuildingCode)?.Name ?? "",
                HomeName = homes?.FirstOrDefault(h => h.Code == selectedHomeCode)?.Name ?? ""
            };

            StateHasChanged();
        }

        private class SelectedAddress
        {
            public string CityName { get; set; } = "";
            public string TownName { get; set; } = "";
            public string NeighboorName { get; set; } = "";
            public string StreetName { get; set; } = "";
            public string BuildingName { get; set; } = "";
            public string HomeName { get; set; } = "";
        }
    }
}
