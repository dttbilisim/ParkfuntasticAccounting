using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface ICustomerAccountTransactionService
    {
        /// <summary>
        /// Müşterinin cari hesap hareketlerini getirir
        /// </summary>
        Task<IActionResult<List<CustomerAccountTransactionListDto>>> GetCustomerAccountTransactions(
            int customerId, 
            DateTime? startDate = null, 
            DateTime? endDate = null);

        /// <summary>
        /// Müşterinin cari hesap bakiyesini getirir (Toplam Borç - Toplam Alacak)
        /// </summary>
        Task<IActionResult<decimal>> GetCustomerBalance(int customerId);

        /// <summary>
        /// Müşterinin cari hesap raporunu getirir (bakiye + hareket listesi)
        /// </summary>
        Task<IActionResult<CustomerAccountReportDto>> GetCustomerAccountReport(
            int customerId, 
            DateTime? startDate = null, 
            DateTime? endDate = null);

        /// <summary>
        /// Yeni cari hesap hareketi oluşturur. Başarıda Result = oluşturulan hareket Id.
        /// </summary>
        Task<IActionResult<int>> CreateTransaction(AuditWrapDto<CustomerAccountTransactionUpsertDto> model);

        /// <summary>
        /// Plasiyerin bağlı olduğu tüm müşterilerin toplam bakiyesini getirir
        /// </summary>
        Task<IActionResult<CustomerAccountReportDto>> GetPlasiyerAccountSummary(int userId);

        /// <summary>
        /// Tahsilat makbuzu gönderimi için hareketi doğrular ve makbuz verisini döner.
        /// Sadece alacak (Credit) ve açıklamasında "tahsilat" geçen hareketler için geçerlidir.
        /// salesPersonId verilirse plasiyer bazlı makbuz no getirilir/oluşturulur.
        /// </summary>
        Task<IActionResult<PaymentReceiptDto>> GetTransactionForReceipt(int transactionId, int customerId, int? salesPersonId = null);

        /// <summary>
        /// Tüm cari hareketleri sayfalı getirir (B2B Admin). Tenant (BranchId) filtresi uygulanır.
        /// FilterCustomers ve CustomerSubtotals backend'de doldurulur; filtreler listeden beslenir.
        /// </summary>
        Task<IActionResult<CustomerAccountTransactionsPageResult>> GetPagedAllCustomerAccountTransactions(
            PageSetting pager,
            int? customerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}
