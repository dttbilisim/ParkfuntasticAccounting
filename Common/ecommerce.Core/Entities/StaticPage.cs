using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities
{
    public class StaticPage : AuditableEntity<int>
    {
        public StaticPageType StaticPageType { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public string? FileName { get; set; }
        public string? Root { get; set; }
        public string? FileGuid { get; set; }
    }
}

