using ecommerce.Core.Models;
using ecommerce.Admin.Domain.Dtos.AppSettingDto;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Helpers;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IAppSettingService
    {
        public Task<IActionResult<AppSettings>> GetValue(string key);
        public Task<IActionResult<List<AppSettings>>> GetValues(params string[] key);
        
        Task<IActionResult<Paging<List<AppSettings>>>> GetSettings(int page, int pageSize);
        Task<IActionResult<AppSettingUpsertDto>> GetSettingById(int id);
        Task<IActionResult<AppSettings>> UpsertSetting(AuditWrapDto<AppSettingUpsertDto> setting);
    }
}
