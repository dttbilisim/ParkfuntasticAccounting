using ecommerce.Admin.Domain.Dtos.PaymentTypeDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces;

public interface IPaymentTypeService
{
    Task<IActionResult<Paging<IQueryable<PaymentTypeListDto>>>> GetPaymentTypes(PageSetting pager);
    Task<IActionResult<List<PaymentTypeListDto>>> GetAllPaymentTypes();
    Task<IActionResult<List<PaymentTypeListDto>>> GetAllPaymentTypesForInvoice();
    Task<IActionResult<PaymentTypeUpsertDto>> GetPaymentTypeById(int id);
    Task<IActionResult<Empty>> UpsertPaymentType(AuditWrapDto<PaymentTypeUpsertDto> wrap);
    Task<IActionResult<Empty>> DeletePaymentType(AuditWrapDto<PaymentTypeDeleteDto> wrap);
}
