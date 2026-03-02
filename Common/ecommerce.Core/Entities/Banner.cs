using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
namespace ecommerce.Core.Entities
{
    public class Banner : AuditableEntity<int>
    {
        public BannerType BannerType { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public bool AutoLoop{get;set;} = false;
        public long AutoStartTime{get;set;} = 0;
        public long ReplayTime{get;set;} = 0;
        public int BannerCount{get;set;} = 0;
        public int? BranchId { get; set; }
       

    }
}
