using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
namespace ecommerce.Core.Entities
{
    public class NotificationEvent : AuditableEntity<int>
    {
        /// <summary>
        /// Sms mi Email mi bilgisini tutar.
        /// </summary>
        public NotificationTypeList NotificationType { get; set; }
        /// <summary>
        /// Sms veya Email içerik bilgisidir
        /// </summary>
        public string Template { get; set; }
        
        /// <summary>
        /// Sms veya Email bildiriminin kime gideceği bilgisidir. Burası tel no veya gönderilecek email bilgisini tutar
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Bildirimin gönderilip gönderilmediği bilgisi
        /// Null: Gönderilecek, True: Gönderildi, False:Gönderilemedi Bilgisidir.
        /// </summary>
        public bool? NotificationStatus { get; set; }
    }
}
