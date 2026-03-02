using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace ecommerce.Web.Controllers;

[ApiController]
[Route("sitemap.xml")]
[OutputCache(PolicyName = "StaticPages")]
public class SitemapController : ControllerBase
{
    private readonly ISellerProductService _productService;
    public SitemapController(ISellerProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        Response.ContentType = "application/xml";
        var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
        var urls = new List<(string loc, DateTime? lastmod, string? changefreq, decimal? priority)>
        {
            ($"{baseUrl}/", null, "daily", 0.8m),
            ($"{baseUrl}/cart", null, "weekly", 0.4m),
            ($"{baseUrl}/search", null, "daily", 0.6m)
        };

        try
        {
            const int pageSize = 200;
            int page = 1;
            int maxItems = 2000;
            int added = 0;
            while (added < maxItems)
            {
                var result = await _productService.GetAllAsync(page, pageSize);
                if (result.Ok != true || result.Result == null || result.Result.Count == 0) break;
                foreach (var p in result.Result)
                {
                    var sid = p.SellerItemId > 0 ? p.SellerItemId : p.ProductId;
                    var brandId = p.Brand?.Id;
                    var link = brandId.HasValue
                        ? $"{baseUrl}/product-detail?productId={sid}&brandId={brandId}"
                        : $"{baseUrl}/product-detail?productId={sid}";
                    urls.Add((link, p.SellerModifiedDate, "weekly", 0.6m));
                    added++;
                    if (added >= maxItems) break;
                }
                if (result.Result.Count < pageSize) break;
                page++;
            }
        }
        catch
        {
            // ignore data errors, return at least core urls
        }

        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var stream = new MemoryStream();
        using (var writer = System.Xml.XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var (loc, lastmod, changefreq, priority) in urls)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", loc);
                if (lastmod.HasValue) writer.WriteElementString("lastmod", lastmod.Value.ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(changefreq)) writer.WriteElementString("changefreq", changefreq);
                if (priority.HasValue) writer.WriteElementString("priority", priority.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        var xml = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        return Content(xml, "application/xml");
    }
}