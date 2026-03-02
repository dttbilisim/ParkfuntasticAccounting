using ecommerce.Web.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace ecommerce.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[OutputCache(PolicyName = "StaticPages")]
public class ManufacturerLogosController : ControllerBase
{
    private readonly IManufacturerCacheService _manufacturerService;

    public ManufacturerLogosController(IManufacturerCacheService manufacturerService)
    {
        _manufacturerService = manufacturerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogos()
    {
        var result = await _manufacturerService.GetAllAsync();
        if (!result.Ok || result.Result == null)
        {
            return Ok(new Dictionary<string, string>());
        }

        var logoMap = result.Result
            .Where(m => !string.IsNullOrEmpty(m.LogoUrl))
            .ToDictionary(
                m => m.Name, 
                m => m.LogoUrl
            );

        return Ok(logoMap);
    }
}
