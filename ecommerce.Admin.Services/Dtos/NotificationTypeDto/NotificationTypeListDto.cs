using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.NotificationTypeDto
{
    [AutoMap(typeof(NotificationType))]
    public class NotificationTypeListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public NotificationTypeList NotificationTypeList { get; set; }
    }
}
