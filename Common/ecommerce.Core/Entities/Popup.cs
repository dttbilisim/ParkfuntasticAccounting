using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities
{
    public class Popup : AuditableEntity<int>
    {
        public string Name { get; set; } = null!;

        public string? Title { get; set; }

        public string? Body { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int Order { get; set; }

        public PopupTrigger Trigger { get; set; }

        public string? TriggerReference { get; set; }

        public int TimeExpire { get; set; }

        public bool IsOnlyImage { get; set; }

        public string? Width { get; set; }

        public string? Height { get; set; }

        public Rule? Rule { get; set; }
    }
}