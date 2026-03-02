using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

/// <summary>
/// Tahsilat makbuzu servisi — plasiyer bazlı otomatik makbuz numarası ve makbuz kaydı.
/// </summary>
public interface ICollectionReceiptService
{
    /// <summary>
    /// Plasiyer için bir sonraki makbuz numarasını üretir (TM-{SalesPersonId}-{Year}-{Seq}).
    /// </summary>
    Task<string> GetNextMakbuzNoAsync(int salesPersonId);

    /// <summary>
    /// Bu tahsilat hareketi için makbuz kaydı varsa döndürür, yoksa oluşturur ve makbuz numarasını döndürür.
    /// </summary>
    Task<IActionResult<string>> GetOrCreateMakbuzNoAsync(int customerAccountTransactionId, int customerId, int salesPersonId, int? branchId, int userId);

    /// <summary>
    /// Tahsilat hareketi için makbuz kaydı oluşturur (tahsilat alındığında çağrılır).
    /// </summary>
    Task<IActionResult<string>> CreateReceiptAsync(int customerAccountTransactionId, int customerId, int salesPersonId, int? branchId, int userId);

    /// <summary>
    /// Hareket ID ile mevcut makbuz numarasını döndürür (yoksa null).
    /// </summary>
    Task<string?> GetMakbuzNoByTransactionIdAsync(int customerAccountTransactionId);
}
