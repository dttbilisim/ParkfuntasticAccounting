using ecommerce.Core.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;
namespace ecommerce.Core.Entities
{
    public class BannerItem : AuditableEntity<int>
    {
        public int BannerId { get; set; }
        [ForeignKey("BannerId")]
        public virtual Banner Banner { get; set; }
         
        public int Order { get; set; }
        public string ? FileGuid { get; set; }
        public string ? FileName { get; set; }
        public string ? FileNameMobile{get;set;}
        public string Root { get; set; }
        public string ? MobileImageUrl{get;set;}

        public string Title { get; set; }
        public string? Description { get; set; }
        public string Url { get; set; }
        public bool IsNewTab { get; set; }
        public bool IsButton { get; set; }
        public string? ButtonName { get; set; }
        public int BannerCount{get;set;} = 0;
        public bool FullSlider{get;set;}
        public bool IsVideo{get;set;}
        public string ? VideoUrl{get;set;}

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ICollection<BannerSubItem> BannerSubItems { get; set; }

    }
}
