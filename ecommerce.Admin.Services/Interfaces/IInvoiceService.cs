using ecommerce.Admin.Domain.Dtos.InvoiceDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

public interface IInvoiceService
{
    Task<IActionResult<Paging<IQueryable<InvoiceListDto>>>> GetInvoices(PageSetting pager, int? invoiceTypeId = null);
    Task<IActionResult<InvoiceUpsertDto>> GetInvoiceById(int id);
    Task<IActionResult<Empty>> UpsertInvoice(AuditWrapDto<InvoiceUpsertDto> model);
    Task<IActionResult<Empty>> DeleteInvoice(AuditWrapDto<InvoiceDeleteDto> model);
    /// <summary>
    /// Müşterinin faturalarını getirir
    /// </summary>
    Task<IActionResult<List<InvoiceListDto>>> GetCustomerInvoices(int customerId);
    
    /// <summary>
    /// Plasiyerin bağlı olduğu tüm müşterilerin faturalarını getirir
    /// </summary>
    Task<IActionResult<List<InvoiceListDto>>> GetPlasiyerCustomersInvoices(int userId);

    /// <summary>
    /// e-Fatura gönderimi sonrası ETTN, durum ve fatura tipi bilgisini günceller
    /// </summary>
    Task<IActionResult<Empty>> UpdateEInvoiceStatus(int invoiceId, string ettn, string status, bool isEInvoice, bool isEArchive, int userId);

    /// <summary>
    /// e-Fatura iptal sonrası ETTN, durum ve fatura tipi bilgilerini temizler
    /// </summary>
    Task<IActionResult<Empty>> ClearEInvoiceStatus(int invoiceId, int userId);
}
