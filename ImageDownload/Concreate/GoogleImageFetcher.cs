using ImageDownload.Abstract;
using ImageDownload.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
namespace ImageDownload.Concreate;
public class GoogleImageFetcher : IGoogleImageFetcher
{
    private readonly GoogleImageFetcherOptions _options;
    private readonly ILogger<GoogleImageFetcher> _logger;

    public GoogleImageFetcher(IOptions<GoogleImageFetcherOptions> options, ILogger<GoogleImageFetcher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> GetFirstImageUrlAsync(string query)
    {
        try
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox" }
            });

            using var page = await browser.NewPageAsync();
            await page.GoToAsync($"https://www.google.com/search?tbm=isch&q={Uri.EscapeDataString(query)}");

            await page.WaitForSelectorAsync("img");

            var imageUrl = await page.EvaluateFunctionAsync<string>(
                "() => { const img = document.querySelectorAll('img')[1]; return img?.src || null; }"
            );

            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching image URL from Google for query: {Query}", query);
            return null;
        }
    }
}