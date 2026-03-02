using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>
/// Tahsilat makbuzu PDF dosyası üretir.
/// </summary>
public interface IPaymentReceiptPdfService
{
    /// <summary>
    /// Tahsilat makbuzu PDF'ini üretir.
    /// </summary>
    /// <param name="receipt">Makbuz verisi</param>
    /// <returns>PDF dosya baytları</returns>
    byte[] GeneratePdf(PaymentReceiptDto receipt);
}
