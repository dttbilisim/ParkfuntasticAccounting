using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.NotificationEventDto
{
    [AutoMap(typeof(NotificationEvent), ReverseMap = true)]
    public class NotificationEventUpsertDto
    {
        public int? Id { get; set; }
        public NotificationTypeList NotificationType { get; set; }
        public string Template { get; set; }
        public string Value { get; set; }
        public bool? NotificationStatus { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
