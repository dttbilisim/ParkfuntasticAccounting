using ecommerce.Admin.Domain.Dtos.PaymentCollection;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>
/// Plasiyer ödeme alma servisi arayüzü
/// </summary>
public interface IPaymentCollectionService
{
    /// <summary>
    /// Carinin faturalaşmamış siparişlerini getirir (plasiyer yetki kontrolü dahil)
    /// </summary>
    Task<IActionResult<List<UnfacturedOrderMobileDto>>> GetUnfacturedOrdersByCustomer(int customerId, int salesPersonId);

    /// <summary>
    /// Nakit ödeme alır — CustomerAccountTransaction tablosuna Credit kaydı oluşturur
    /// </summary>
    Task<IActionResult<CollectPaymentResultDto>> CollectCashPayment(CollectPaymentRequestDto request, int salesPersonId, int userId);

    /// <summary>
    /// Sanal POS (kredi kartı) ödeme alır — PaymentProviderFactory üzerinden işlem başlatır
    /// </summary>
    Task<IActionResult<CollectPaymentResultDto>> CollectCardPayment(CollectPaymentRequestDto request, int salesPersonId, int userId);
}
