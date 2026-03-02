using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.NotificationTypeDto
{
    [AutoMap(typeof(NotificationType), ReverseMap = true)]
    public class NotificationTypeUpsertDto
    {
        public int? Id { get; set; }
        public NotificationTypeList NotificationTypeList { get; set; }
        public string Name { get; set; }
        public bool Value { get; set; } = false;
    }
}
