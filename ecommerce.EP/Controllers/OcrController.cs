using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ecommerce.EP.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ocr")]
public class OcrController : ControllerBase
{
    private const string DefaultOcrSpaceUrl = "https://api.ocr.space/parse/image";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcrController> _logger;

    public OcrController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OcrController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("parse")]
    public async Task<IActionResult> ParseImage([FromBody] OcrRequest request)
    {
        var apiKey = _configuration["Ocr:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Ocr:ApiKey yapılandırılmamış.");
            return StatusCode(503, new { error = "OCR servisi yapılandırılmamış." });
        }

        try
        {
            _logger.LogDebug("OCR isteği - Language: {Language}, Engine: {Engine}", request.Language, request.OcrEngine);

            var apiUrl = _configuration["Ocr:ApiUrl"] ?? DefaultOcrSpaceUrl;
            var timeoutSeconds = _configuration.GetValue("Ocr:TimeoutSeconds", 60);
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(apiKey), "apikey");
            formData.Add(new StringContent(request.Base64Image ?? string.Empty), "base64Image");
            formData.Add(new StringContent(request.Language ?? "tur"), "language");
            formData.Add(new StringContent("false"), "isOverlayRequired");
            formData.Add(new StringContent("true"), "detectOrientation");
            formData.Add(new StringContent("true"), "scale");
            formData.Add(new StringContent(request.OcrEngine?.ToString() ?? "2"), "OCREngine");

            var response = await httpClient.PostAsync(apiUrl, formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("OCR.space yanıtı - Status: {StatusCode}, Length: {Length}", response.StatusCode, responseContent.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ OCR.space API hatası - Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                return StatusCode((int)response.StatusCode, new { error = "OCR API hatası", details = responseContent });
            }

            return Content(responseContent, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ OCR işlemi sırasında hata oluştu");
            return StatusCode(500, new { error = "OCR işlemi başarısız", message = ex.Message });
        }
    }
}

public class OcrRequest
{
    public string? Base64Image { get; set; }
    public string? Language { get; set; }
    public int? OcrEngine { get; set; }
}
