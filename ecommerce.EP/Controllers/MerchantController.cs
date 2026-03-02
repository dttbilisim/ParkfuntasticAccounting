using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.Odaksodt.Abstract;
using System.Security.Claims;

namespace ecommerce.EP.Controllers;

/// <summary>
/// Tahsilat makbuzu gönderim kanalı: email = e-posta ile PDF ekli gönder, whatsapp = PDF base64 döner (uygulama WhatsApp ile paylaşır).
/// </summary>
public class SendCollectionReceiptRequest
{
    public int TransactionId { get; set; }
    public string Channel { get; set; } = "email"; // "email" | "whatsapp"
    /// <summary>E-posta kanalında kullanılacak adres; boşsa carinin e-postası kullanılır.</summary>
    public string? Email { get; set; }
}

/// <summary>
/// Tüccar (merchant) işlemleri — kendi cari hesap raporu vb.
/// </summary>
[Authorize(Roles = "CustomerB2B")]
[Route("api/[controller]")]
[ApiController]
public class MerchantController : ControllerBase
{
    private readonly ICustomerAccountTransactionService _customerAccountTransactionService;
    private readonly IOdaksoftInvoiceService _odaksoftInvoiceService;
    private readonly IEmailService _emailService;
    private readonly IPaymentReceiptPdfService _paymentReceiptPdfService;
    private readonly ILogger<MerchantController> _logger;

    public MerchantController(
        ICustomerAccountTransactionService customerAccountTransactionService,
        IOdaksoftInvoiceService odaksoftInvoiceService,
        IEmailService emailService,
        IPaymentReceiptPdfService paymentReceiptPdfService,
        ILogger<MerchantController> logger)
    {
        _customerAccountTransactionService = customerAccountTransactionService;
        _odaksoftInvoiceService = odaksoftInvoiceService;
        _emailService = emailService;
        _paymentReceiptPdfService = paymentReceiptPdfService;
        _logger = logger;
    }

    /// <summary>
    /// Tüccarın kendi cari hesap raporunu getirir (borç-alacak ekranı için)
    /// </summary>
    [HttpGet("my-account-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyAccountReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        // JWT claim'lerinden CustomerId al
        var customerIdClaim = User.FindFirst("CustomerId");
        if (customerIdClaim == null || !int.TryParse(customerIdClaim.Value, out var customerId) || customerId <= 0)
        {
            _logger.LogWarning("Tüccar CustomerId bulunamadı. UserId: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return BadRequest(new { message = "Müşteri bilgisi bulunamadı." });
        }

        try
        {
            var response = await _customerAccountTransactionService.GetCustomerAccountReport(
                customerId,
                startDate,
                endDate
            );

            if (response.Ok && response.Result != null)
            {
                return Ok(response.Result);
            }

            _logger.LogError("Cari hesap raporu alınamadı. CustomerId: {CustomerId}", customerId);
            return NotFound(new { message = response.Metadata?.Message ?? "Cari hesap raporu bulunamadı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cari hesap raporu alınırken hata oluştu. CustomerId: {CustomerId}", customerId);
            return StatusCode(500, new { message = "Cari hesap raporu yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// e-Fatura PDF'ini indirir (ETTN ile)
    /// </summary>
    [HttpGet("download-invoice/{ettn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadInvoice(string ettn)
    {
        if (string.IsNullOrWhiteSpace(ettn))
        {
            return BadRequest(new { message = "ETTN gereklidir." });
        }

        try
        {
            var response = await _odaksoftInvoiceService.DownloadOutboxInvoiceAsync(ettn);

            if (response.Status && !string.IsNullOrEmpty(response.ByteArray))
            {
                return Ok(new
                {
                    success = true,
                    base64Data = response.ByteArray,
                    fileName = $"Fatura_{ettn}.pdf",
                    mimeType = "application/pdf"
                });
            }
            else if (response.Status && !string.IsNullOrEmpty(response.Html))
            {
                return Ok(new
                {
                    success = true,
                    html = response.Html,
                    fileName = $"Fatura_{ettn}.html",
                    mimeType = "text/html"
                });
            }

            _logger.LogError("Fatura indirilemedi. ETTN: {Ettn}, Message: {Message}", ettn, response.Message);
            return NotFound(new { message = response.Message ?? response.ExceptionMessage ?? "Fatura bulunamadı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura indirilirken hata oluştu. ETTN: {Ettn}", ettn);
            return StatusCode(500, new { message = "Fatura indirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Tahsilat makbuzu gönderir: email = e-posta ile PDF ekli, whatsapp = PDF base64 döner (uygulama WhatsApp ile paylaşır).
    /// </summary>
    [HttpPost("send-collection-receipt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendCollectionReceipt([FromBody] SendCollectionReceiptRequest request)
    {
        var customerIdClaim = User.FindFirst("CustomerId");
        if (customerIdClaim == null || !int.TryParse(customerIdClaim.Value, out var customerId) || customerId <= 0)
        {
            return BadRequest(new { message = "Müşteri bilgisi bulunamadı." });
        }

        if (request?.TransactionId <= 0)
        {
            return BadRequest(new { message = "Geçerli bir hareket ID gereklidir." });
        }

        var channel = (request?.Channel ?? "email").ToLowerInvariant();
        if (channel != "email" && channel != "whatsapp")
        {
            return BadRequest(new { message = "Kanal 'email' veya 'whatsapp' olmalıdır." });
        }

        var receiptResult = await _customerAccountTransactionService.GetTransactionForReceipt(request.TransactionId, customerId);
        if (!receiptResult.Ok || receiptResult.Result == null)
        {
            return BadRequest(new { message = receiptResult.Metadata?.Message ?? "Makbuz gönderilemedi." });
        }

        var receipt = receiptResult.Result;
        var pdfBytes = _paymentReceiptPdfService.GeneratePdf(receipt);
        var fileName = $"TahsilatMakbuzu_{receipt.TransactionDate:yyyyMMdd}_{receipt.TransactionDate:HHmmss}.pdf";

        if (channel == "email")
        {
            var toEmail = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.Trim() : receipt.CustomerEmail;
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return BadRequest(new { message = "E-posta adresi girilmedi ve caride e-posta tanımlı değil." });
            }
            try
            {
                await _emailService.SendPaymentReceiptEmail(
                    toEmail,
                    receipt.CustomerName,
                    receipt.CustomerName,
                    receipt.CustomerCode,
                    receipt.TransactionDate,
                    receipt.Description,
                    receipt.Amount,
                    receipt.PaymentTypeName,
                    pdfAttachment: pdfBytes,
                    pdfFileName: fileName,
                    makbuzNo: receipt.MakbuzNo);
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { success = false, message = "E-posta gönderilemedi: " + ex.Message });
            }

            return Ok(new { success = true, message = "Tahsilat makbuzu e-posta ile gönderildi." });
        }

        // whatsapp: PDF base64 döndür, uygulama paylaşım menüsü ile WhatsApp'a gönderir
        var base64Pdf = Convert.ToBase64String(pdfBytes);
        return Ok(new
        {
            success = true,
            message = "Makbuz hazır. WhatsApp ile paylaşabilirsiniz.",
            base64Data = base64Pdf,
            fileName,
            mimeType = "application/pdf"
        });
    }
}
