using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.PaymentCollection;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.Odaksodt.Abstract;
using System.Security.Claims;

namespace ecommerce.EP.Controllers;

/// <summary>
/// Plasiyer (satış temsilcisi) işlemleri — cari listesi, cari seçimi, ödeme alma vb.
/// </summary>
[Authorize(Roles = "Plasiyer")]
[Route("api/[controller]")]
[ApiController]
public class SalesmanController : ControllerBase
{
    private readonly ISalesPersonService _salesPersonService;
    private readonly ICustomerAccountTransactionService _customerAccountTransactionService;
    private readonly IPaymentCollectionService _paymentCollectionService;
    private readonly IOdaksoftInvoiceService _odaksoftInvoiceService;
    private readonly IEmailService _emailService;
    private readonly IPaymentReceiptPdfService _paymentReceiptPdfService;
    private readonly ILogger<SalesmanController> _logger;

    public SalesmanController(
        ISalesPersonService salesPersonService,
        ICustomerAccountTransactionService customerAccountTransactionService,
        IPaymentCollectionService paymentCollectionService,
        IOdaksoftInvoiceService odaksoftInvoiceService,
        IEmailService emailService,
        IPaymentReceiptPdfService paymentReceiptPdfService,
        ILogger<SalesmanController> logger)
    {
        _salesPersonService = salesPersonService;
        _customerAccountTransactionService = customerAccountTransactionService;
        _paymentCollectionService = paymentCollectionService;
        _odaksoftInvoiceService = odaksoftInvoiceService;
        _emailService = emailService;
        _paymentReceiptPdfService = paymentReceiptPdfService;
        _logger = logger;
    }

    /// <summary>
    /// Plasiyere bağlı carileri getirir.
    /// </summary>
    [HttpGet("customers")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CustomerListDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomers()
    {
        // JWT claim'lerinden SalesPersonId al
        var salesPersonIdClaim = User.FindFirst("SalesPersonId");
        if (salesPersonIdClaim == null || !int.TryParse(salesPersonIdClaim.Value, out var salesPersonId) || salesPersonId <= 0)
        {
            _logger.LogWarning("Plasiyer SalesPersonId bulunamadı. UserId: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return BadRequest(new { message = "Plasiyer bilgisi bulunamadı." });
        }

        try
        {
            var response = await _salesPersonService.GetCustomersOfSalesPerson(salesPersonId);
            if (response.Ok)
            {
                return Ok(response.Result);
            }

            _logger.LogError("Cari listesi alınamadı. SalesPersonId: {SalesPersonId}", salesPersonId);
            return NotFound(new { message = "Müşteri listesi bulunamadı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cari listesi alınırken hata oluştu. SalesPersonId: {SalesPersonId}", salesPersonId);
            return StatusCode(500, new { message = "Müşteri listesi yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Harita ekranı için plasiyere bağlı carileri adres bilgileriyle getirir.
    /// Mobil taraf adres bilgisinden geocoding yapacak.
    /// </summary>
    [HttpGet("customers-with-coords")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CustomerWithCoordsDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomersWithCoords()
    {
        // JWT claim'lerinden SalesPersonId al
        var salesPersonIdClaim = User.FindFirst("SalesPersonId");
        if (salesPersonIdClaim == null || !int.TryParse(salesPersonIdClaim.Value, out var salesPersonId) || salesPersonId <= 0)
        {
            _logger.LogWarning("Plasiyer SalesPersonId bulunamadı. UserId: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return BadRequest(new { message = "Plasiyer bilgisi bulunamadı." });
        }

        try
        {
            var response = await _salesPersonService.GetCustomersWithCoordsOfSalesPerson(salesPersonId);
            if (response.Ok)
            {
                return Ok(response.Result);
            }

            _logger.LogError("Harita için cari listesi alınamadı. SalesPersonId: {SalesPersonId}", salesPersonId);
            return NotFound(new { message = "Müşteri listesi bulunamadı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Harita için cari listesi alınırken hata oluştu. SalesPersonId: {SalesPersonId}", salesPersonId);
            return StatusCode(500, new { message = "Müşteri listesi yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Müşterinin cari hesap raporunu getirir (borç-alacak ekranı için)
    /// </summary>
    [HttpGet("customer-account-report/{customerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerAccountReport(
        int customerId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        if (customerId <= 0)
        {
            return BadRequest(new { message = "Geçersiz müşteri ID." });
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

            _logger.LogError("Müşteri hesap raporu alınamadı. CustomerId: {CustomerId}", customerId);
            return NotFound(new { message = response.Metadata?.Message ?? "Müşteri hesap raporu bulunamadı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Müşteri hesap raporu alınırken hata oluştu. CustomerId: {CustomerId}", customerId);
            return StatusCode(500, new { message = "Müşteri hesap raporu yüklenirken bir hata oluştu." });
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
    /// Carinin faturalaşmamış siparişlerini getirir (plasiyer yetki kontrolü dahil)
    /// </summary>
    [HttpGet("unfactured-orders/{customerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnfacturedOrders(int customerId)
    {
        // JWT claim'lerinden SalesPersonId al
        var salesPersonIdClaim = User.FindFirst("SalesPersonId");
        if (salesPersonIdClaim == null || !int.TryParse(salesPersonIdClaim.Value, out var salesPersonId) || salesPersonId <= 0)
        {
            _logger.LogWarning("Plasiyer SalesPersonId bulunamadı. UserId: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return BadRequest(new { message = "Plasiyer bilgisi bulunamadı." });
        }

        if (customerId <= 0)
        {
            return BadRequest(new { message = "Geçersiz müşteri ID." });
        }

        try
        {
            var response = await _paymentCollectionService.GetUnfacturedOrdersByCustomer(customerId, salesPersonId);

            if (response.Ok)
            {
                return Ok(response.Result);
            }

            // Yetki hatası kontrolü
            var errorMsg = response.Metadata?.Message ?? "Siparişler yüklenirken bir hata oluştu.";
            if (errorMsg.Contains("yetki", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new { message = errorMsg });
            }

            return BadRequest(new { message = errorMsg });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Faturalaşmamış siparişler alınırken hata. CustomerId: {CustomerId}, SalesPersonId: {SalesPersonId}", customerId, salesPersonId);
            return StatusCode(500, new { message = "Siparişler yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Plasiyer ödeme alma — nakit veya sanal POS
    /// </summary>
    [HttpPost("collect-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CollectPayment([FromBody] CollectPaymentRequestDto request)
    {
        // JWT claim'lerinden SalesPersonId al
        var salesPersonIdClaim = User.FindFirst("SalesPersonId");
        if (salesPersonIdClaim == null || !int.TryParse(salesPersonIdClaim.Value, out var salesPersonId) || salesPersonId <= 0)
        {
            _logger.LogWarning("Plasiyer SalesPersonId bulunamadı. UserId: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return BadRequest(new { message = "Plasiyer bilgisi bulunamadı." });
        }

        // UserId al
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return BadRequest(new { message = "Kullanıcı bilgisi bulunamadı." });
        }

        // Temel validasyon
        if (request.CustomerId <= 0)
            return BadRequest(new { message = "Geçersiz müşteri ID." });

        if (request.Amount <= 0)
            return BadRequest(new { message = "Ödeme tutarı sıfırdan büyük olmalıdır." });

        try
        {
            _logger.LogInformation("Ödeme alma isteği. SalesPersonId: {SalesPersonId}, CustomerId: {CustomerId}, PaymentType: {PaymentType}, Amount: {Amount}, OrderCount: {OrderCount}",
                salesPersonId, request.CustomerId, request.PaymentType, request.Amount, request.OrderIds?.Count ?? 0);

            // Ödeme tipine göre ilgili metodu çağır
            var response = request.PaymentType == ecommerce.Core.Utils.BankPaymentType.Nakit
                ? await _paymentCollectionService.CollectCashPayment(request, salesPersonId, userId)
                : await _paymentCollectionService.CollectCardPayment(request, salesPersonId, userId);

            if (response.Ok && response.Result?.Success == true)
            {
                _logger.LogInformation("Ödeme başarılı. SalesPersonId: {SalesPersonId}, CustomerId: {CustomerId}, Amount: {Amount}",
                    salesPersonId, request.CustomerId, request.Amount);
                return Ok(response.Result);
            }

            // Hata durumu
            var errorMsg = response.Metadata?.Message ?? response.Result?.Message ?? "Ödeme işlemi sırasında bir hata oluştu.";
            if (errorMsg.Contains("yetki", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new { message = errorMsg });
            }

            return BadRequest(new { message = errorMsg });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ödeme alma sırasında hata. SalesPersonId: {SalesPersonId}, CustomerId: {CustomerId}", salesPersonId, request.CustomerId);
            return StatusCode(500, new { message = "Ödeme işlemi sırasında bir hata oluştu." });
        }
    }

    /// <summary>
    /// Cariye tahsilat makbuzu gönderir: email = e-posta ile PDF ekli, whatsapp = PDF base64 döner (uygulama WhatsApp ile paylaşır).
    /// </summary>
    [HttpPost("customer-account-report/{customerId}/send-collection-receipt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendCollectionReceipt(int customerId, [FromBody] SendCollectionReceiptRequest request)
    {
        if (customerId <= 0)
        {
            return BadRequest(new { message = "Geçersiz müşteri ID." });
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

        var salesPersonIdClaim = User.FindFirst("SalesPersonId");
        int? salesPersonId = salesPersonIdClaim != null && int.TryParse(salesPersonIdClaim.Value, out var spId) ? spId : null;

        var receiptResult = await _customerAccountTransactionService.GetTransactionForReceipt(request.TransactionId, customerId, salesPersonId);
        if (!receiptResult.Ok || receiptResult.Result == null)
        {
            return BadRequest(new { message = receiptResult.Metadata?.Message ?? "Makbuz gönderilemedi." });
        }

        var receipt = receiptResult.Result;
        var pdfBytes = _paymentReceiptPdfService.GeneratePdf(receipt);
        var fileName = !string.IsNullOrWhiteSpace(receipt.MakbuzNo)
            ? $"TahsilatMakbuzu_{receipt.MakbuzNo}.pdf"
            : $"TahsilatMakbuzu_{receipt.TransactionDate:yyyyMMdd}_{receipt.TransactionDate:HHmmss}.pdf";

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
