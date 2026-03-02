using ecommerce.Admin.Domain.Dtos.InvoiceTypeDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IInvoiceTypeService
    {
        public Task<IActionResult<Paging<IQueryable<InvoiceTypeListDto>>>> GetInvoiceTypes(PageSetting pager);
        public Task<IActionResult<List<InvoiceTypeListDto>>> GetInvoiceTypes();
        public Task<IActionResult<Empty>> UpsertInvoiceType(AuditWrapDto<InvoiceTypeUpsertDto> model);
        public Task<IActionResult<Empty>> DeleteInvoiceType(AuditWrapDto<InvoiceTypeDeleteDto> model);
        public Task<IActionResult<InvoiceTypeUpsertDto>> GetInvoiceTypeById(int id);
        
        /// <summary>
        /// Fatura tipi listesini yetki kontrolü olmadan, sadece BranchId filtreleme ile döndürür.
        /// Fatura oluşturma modal'ı gibi yerlerden çağrılır.
        /// </summary>
        public Task<IActionResult<List<InvoiceTypeListDto>>> GetInvoiceTypesForInvoice();
    }
}

