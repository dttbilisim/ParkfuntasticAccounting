using ecommerce.Admin.Domain.Dtos.NotificationEventDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface INotificationEventService
    {
        public Task<IActionResult<Paging<IQueryable<NotificationEventListDto>>>> GetNotificationEvents(PageSetting pager);
        public Task<IActionResult<List<NotificationEventListDto>>> GetNotificationEvents();
        Task<IActionResult<Empty>> UpsertNotificationEvent(AuditWrapDto<NotificationEventUpsertDto> model);
        Task<IActionResult<Empty>> DeleteNotificationEvent(AuditWrapDto<NotificationEventDeleteDto> model);
        Task<IActionResult<NotificationEventUpsertDto>> GetNotificationEventById(int Id);
    }
}
