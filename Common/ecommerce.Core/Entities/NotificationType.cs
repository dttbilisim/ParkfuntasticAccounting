using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities
{
    public class NotificationType:Entity<int>
    {
        public int Id { get; set; }
        public NotificationTypeList NotificationTypeList { get; set; }
        public string Name { get; set; }
        public bool Value { get; set; } = false;

    }
}

