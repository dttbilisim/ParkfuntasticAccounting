using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ecommerce.Admin.Services.Interfaces;

namespace ecommerce.EP.Controllers
{
    /// <summary>
    /// Kampanya ve İndirim Controller — Aktif kampanyaları listeler
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountController : ControllerBase
    {
        private readonly IDiscountCacheService _discountCacheService;
        private readonly ILogger<DiscountController> _logger;

        public DiscountController(
            IDiscountCacheService discountCacheService,
            ILogger<DiscountController> logger)
        {
            _discountCacheService = discountCacheService;
            _logger = logger;
        }

        /// <summary>
        /// Aktif kampanyaları ürünleriyle birlikte getirir (cache'li)
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCampaigns()
        {
            try
            {
                var discounts = await _discountCacheService.GetActiveDiscountsAsync();
                return Ok(discounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif kampanyalar getirilirken hata oluştu");
                return StatusCode(500, new { message = "Kampanyalar yüklenirken bir hata oluştu" });
            }
        }
    }
}
