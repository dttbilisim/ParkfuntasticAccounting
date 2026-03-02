using ecommerce.Odaksodt.Dtos.Gib;
using ecommerce.Odaksodt.Dtos.Invoice;

namespace ecommerce.Odaksodt.Abstract;

/// <summary>
/// Odaksoft fatura servisi interface
/// </summary>
public interface IOdaksoftInvoiceService
{
    /// <summary>
    /// Yeni fatura oluşturur
    /// </summary>
    Task<CreateInvoiceResponseDto> CreateInvoiceAsync(OdaksoftCreateInvoiceRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fatura durumunu sorgular
    /// </summary>
    Task<InvoiceStatusResponseDto> GetInvoiceStatusAsync(InvoiceStatusRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Faturayı PDF olarak indirir
    /// </summary>
    Task<byte[]> DownloadInvoicePdfAsync(string ettn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Faturayı HTML olarak indirir
    /// </summary>
    Task<string> DownloadInvoiceHtmlAsync(string ettn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Taslak faturayı GİB'e gönderir
    /// </summary>
    Task<bool> SendInvoiceToGibAsync(string ettn, CancellationToken cancellationToken = default);

    /// <summary>
    /// E-Arşiv faturayı iptal eder. Başarı durumu ve hata mesajı döner.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> CancelInvoiceAsync(string ettn, string cancelReason, CancellationToken cancellationToken = default);

    /// <summary>
    /// GİB'de kullanıcı tipini sorgular (e-Fatura mükellefi mi?)
    /// </summary>
    Task<CheckUserResponseDto> CheckUserAsync(string vkn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Giden faturayı Odaksoft'tan indirir (PDF byte array olarak döner)
    /// </summary>
    Task<DownloadOutboxInvoiceResponseDto> DownloadOutboxInvoiceAsync(string ettn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gelen faturaları listeler (Inbox)
    /// </summary>
    Task<GetInboxInvoiceResponseDto> GetInboxInvoicesAsync(GetInboxInvoiceRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Giden faturaları filtreli listeler (Outbox)
    /// </summary>
    Task<GetOutboxInvoiceFilterResponseDto> GetOutboxInvoicesAsync(GetOutboxInvoiceFilterRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Giden fatura HTML önizlemesi getirir
    /// </summary>
    Task<OdaksoftPreviewResponseDto> PreviewOutboxInvoiceAsync(string ettn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gelen fatura HTML önizlemesi getirir
    /// </summary>
    Task<OdaksoftPreviewResponseDto> PreviewInboxInvoiceAsync(string ettn, string? refNo = null, CancellationToken cancellationToken = default);
}
