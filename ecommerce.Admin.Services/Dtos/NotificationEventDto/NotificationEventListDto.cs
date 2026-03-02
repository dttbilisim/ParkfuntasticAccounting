using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.NotificationEventDto
{
    public class NotificationEventListDto
    {
        public int Id { get; set; }
        public NotificationTypeList NotificationTypeList { get; set; }
 
        public string Template { get; set; }
 
        public string Value { get; set; }
 
        public bool NotificationStatus { get; set; }
    }
}
