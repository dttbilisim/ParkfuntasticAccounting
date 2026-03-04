using ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface ICashRegisterMovementService
    {
        Task<IActionResult<Paging<List<CashRegisterMovementListDto>>>> GetPaged(
            PageSetting pager,
            int? cashRegisterId = null,
            CashRegisterMovementType? movementType = null,
            CashRegisterMovementProcessType? processType = null,
            int? customerId = null,
            EntityStatusForFilter? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        Task<IActionResult<CashRegisterMovementUpsertDto>> GetById(int id);
        Task<IActionResult<int>> Create(AuditWrapDto<CashRegisterMovementUpsertDto> model);
        Task<IActionResult<Empty>> Update(AuditWrapDto<CashRegisterMovementUpsertDto> model);
        Task<IActionResult<Empty>> Delete(AuditWrapDto<CashRegisterMovementDeleteDto> model);

        /// <summary>
        /// Kasalar arası virman: kaynak kasadan çıkış + hedef kasaya giriş (aynı tutar, aynı tarih).
        /// </summary>
        Task<IActionResult<Empty>> CreateTransfer(AuditWrapDto<CashRegisterTransferDto> model);

        /// <summary>
        /// Kasa(lar) için yürüyen bakiye özeti: açılış, toplam giriş, toplam çıkış, güncel bakiye.
        /// Tarih filtresi opsiyonel; kasa filtresi verilirse sadece o kasa.
        /// </summary>
        Task<IActionResult<List<CashRegisterBalanceSummaryDto>>> GetBalanceSummary(
            int? cashRegisterId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}
