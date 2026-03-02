using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities
{
    public class BannerSubItem : AuditableEntity<int>
    {
        public int BannerItemId { get; set; }
  
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsNewTab { get; set; }
        public int Order { get; set; }
        public string FileGuid { get; set; }
        public string FileName { get; set; }
        public string Root { get; set; }
    }
}
