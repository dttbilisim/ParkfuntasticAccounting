using ecommerce.Admin.Domain.Dtos.NotificationTypeDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface INotificationTypeService
    {
        public Task<IActionResult<Paging<IQueryable<NotificationTypeListDto>>>> GetNotificationTypes(PageSetting pager);
        public Task<IActionResult<List<NotificationTypeListDto>>> GetNotificationTypes();
        Task<IActionResult<Empty>> UpsertNotificationType(AuditWrapDto<NotificationTypeUpsertDto> model);
        Task<IActionResult<Empty>> DeleteNotificationType(AuditWrapDto<NotificationTypeDeleteDto> model);
        Task<IActionResult<NotificationTypeUpsertDto>> GetNotificationTypeById(int Id);

    }
}
