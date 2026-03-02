using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ecommerce.Admin.Domain.Interfaces;

namespace ecommerce.EP.Controllers
{
    /// <summary>
    /// İl/İlçe Yönetimi - Kayıt formu için
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("public")]
    public class LocationController : ControllerBase
    {
        private readonly ICityService _cityService;
        private readonly ITownService _townService;

        public LocationController(ICityService cityService, ITownService townService)
        {
            _cityService = cityService;
            _townService = townService;
        }

        /// <summary>
        /// Tüm illeri getirir
        /// </summary>
        [HttpGet("cities")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCities()
        {
            var result = await _cityService.GetCities();
            
            if (!result.Ok)
            {
                return StatusCode(500, new { message = "İller yüklenirken hata oluştu." });
            }

            return Ok(result.Result);
        }

        /// <summary>
        /// Belirli bir ile ait ilçeleri getirir
        /// </summary>
        [HttpGet("cities/{cityId}/districts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDistricts(int cityId)
        {
            var result = await _townService.GetTownsByCityId(cityId);
            
            if (!result.Ok)
            {
                return StatusCode(500, new { message = "İlçeler yüklenirken hata oluştu." });
            }

            return Ok(result.Result);
        }
    }
}
