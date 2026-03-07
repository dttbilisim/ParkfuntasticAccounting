using ecommerce.Admin.Domain.Dtos.PcPosDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IPcPosService
    {
        public Task<IActionResult<Paging<IQueryable<PcPosListDto>>>> GetPcPos(PageSetting pager);
        public Task<IActionResult<List<PcPosListDto>>> GetPcPos();
        /// <summary>Kullanıcı ataması için PcPos listesi (CanView kontrolü yok - dropdown için)</summary>
        public Task<IActionResult<List<PcPosListDto>>> GetPcPosForUserAssignment();
        public Task<IActionResult<Empty>> UpsertPcPos(AuditWrapDto<PcPosUpsertDto> model);
        public Task<IActionResult<Empty>> DeletePcPos(AuditWrapDto<PcPosDeleteDto> model);
        public Task<IActionResult<PcPosUpsertDto>> GetPcPosById(int id);
    }
}

