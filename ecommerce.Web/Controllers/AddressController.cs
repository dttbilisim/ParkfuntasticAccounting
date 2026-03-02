using Microsoft.AspNetCore.Mvc;
using ecommerce.Web.Domain.Services;

namespace ecommerce.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities()
        {
            try
            {
                var cities = await _addressService.GetCitiesAsync();
                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("towns/{cityId}")]
        public async Task<IActionResult> GetTowns(int cityId)
        {
            try
            {
                var towns = await _addressService.GetTownsAsync(cityId);
                return Ok(towns);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("neighboors/{townId}")]
        public async Task<IActionResult> GetNeighboors(int townId)
        {
            try
            {
                var neighboors = await _addressService.GetNeighboorsAsync(townId);
                return Ok(neighboors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("streets/{neighboorId}")]
        public async Task<IActionResult> GetStreets(int neighboorId)
        {
            try
            {
                var streets = await _addressService.GetStreetsAsync(neighboorId);
                return Ok(streets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("buildings/{streetId}")]
        public async Task<IActionResult> GetBuildings(int streetId)
        {
            try
            {
                var buildings = await _addressService.GetBuildingsAsync(streetId);
                return Ok(buildings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("homes/{buildingId}")]
        public async Task<IActionResult> GetHomes(int buildingId)
        {
            try
            {
                var homes = await _addressService.GetHomesAsync(buildingId);
                return Ok(homes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("address-info/{homeId}")]
        public async Task<IActionResult> GetAddressInfo(int homeId)
        {
            try
            {
                var addressInfo = await _addressService.GetAddressInfoAsync(homeId);
                return Ok(addressInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
