using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Text;
using System.Xml;

namespace ecommerce.Web.Controllers;

/// <summary>
/// Google Merchant Center ürün feed'i için controller
/// </summary>
[ApiController]
[OutputCache(PolicyName = "StaticPages")]
public class GoogleMerchantFeedController : ControllerBase
{
    private readonly IGoogleMerchantService _googleMerchantService;

    public GoogleMerchantFeedController(IGoogleMerchantService googleMerchantService)
    {
        _googleMerchantService = googleMerchantService;
    }

    /// <summary>
    /// Google Merchant Center için XML feed endpoint'i
    /// Google Shopping namespace ile RSS 2.0 formatında döner
    /// Test için maxProducts parametresi desteklenir (varsayılan: 100)
    /// </summary>
    [HttpGet]
    [Route("google-merchant-feed.xml")]
    public async Task<IActionResult> GetXmlFeed([FromQuery] int maxProducts = 100)
    {
        try
        {
            var products = await _googleMerchantService.GetProductsForFeedAsync(maxProducts);

            if (!products.Any())
            {
                return NotFound("No products available for feed");
            }

            // Generate XML
            var xml = GenerateXmlFeed(products);
            
            // Return XML with proper content type and encoding
            return Content(xml, "application/xml", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error generating Google Merchant XML feed: {ex.Message}");
            return StatusCode(500, "Error generating feed");
        }
    }

    /// <summary>
    /// Google Merchant Center Content API için JSON feed endpoint'i
    /// Test için maxProducts parametresi desteklenir (varsayılan: 100)
    /// </summary>
    [HttpGet]
    [Route("api/google-merchant-feed")]
    public async Task<IActionResult> GetJsonFeed([FromQuery] int maxProducts = 100)
    {
        try
        {
            var products = await _googleMerchantService.GetProductsForFeedAsync(maxProducts);
            
            if (!products.Any())
            {
                return NotFound(new { message = "No products available for feed" });
            }
            
            // Return in Google Merchant Content API v2.1 compatible format
            var response = new
            {
                kind = "content#productsCustomBatchResponse",
                resources = products.Select(p => new
                {
                    kind = "content#product",
                    id = p.Id,
                    offerId = p.Id,
                    title = p.Title,
                    description = p.Description,
                    link = p.Link,
                    imageLink = p.ImageLink,
                    contentLanguage = "tr",
                    targetCountry = "TR",
                    channel = "online",
                    availability = p.Availability,
                    condition = p.Condition,
                    price = new
                    {
                        value = p.Price?.Split(' ')[0],
                        currency = p.Price?.Split(' ').LastOrDefault() ?? "TRY"
                    },
                    brand = p.Brand,
                    gtin = p.Gtin
                }).ToList(),
                totalCount = products.Count
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error generating Google Merchant JSON feed: {ex.Message}");
            return StatusCode(500, new { message = "Error generating feed", error = ex.Message });
        }
    }

    /// <summary>
    /// Google Merchant RSS 2.0 formatında XML feed oluştur
    /// </summary>
    private string GenerateXmlFeed(List<ecommerce.Web.Domain.Dtos.GoogleMerchantProductDto> products)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = false
        };

        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            
            // RSS root element with Google namespace
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "g", null, "http://base.google.com/ns/1.0");
            
            // Channel element
            writer.WriteStartElement("channel");
            writer.WriteElementString("title", "Yedeksen Product Feed");
            writer.WriteElementString("link", "https://yedeksen.com");
            writer.WriteElementString("description", "Yedeksen auto parts product feed for Google Merchant Center");
            
            // Items
            foreach (var product in products)
            {
                writer.WriteStartElement("item");
                
                // Required fields
                WriteGoogleElement(writer, "id", product.Id);
                WriteGoogleElement(writer, "title", product.Title);
                WriteGoogleElement(writer, "description", product.Description);
                WriteGoogleElement(writer, "link", product.Link);
                WriteGoogleElement(writer, "image_link", product.ImageLink);
                WriteGoogleElement(writer, "price", product.Price);
                WriteGoogleElement(writer, "availability", product.Availability);
                WriteGoogleElement(writer, "condition", product.Condition);
                WriteGoogleElement(writer, "brand", product.Brand);
                
                // Optional fields
                if (!string.IsNullOrEmpty(product.Gtin))
                {
                    WriteGoogleElement(writer, "gtin", product.Gtin);
                }
                
                writer.WriteEndElement(); // item
            }
            
            writer.WriteEndElement(); // channel
            writer.WriteEndElement(); // rss
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Google namespace elementleri yazmak için yardımcı metod (g:element_name)
    /// </summary>
    private void WriteGoogleElement(XmlWriter writer, string elementName, string value)
    {
        writer.WriteElementString("g", elementName, "http://base.google.com/ns/1.0", value);
    }
}
