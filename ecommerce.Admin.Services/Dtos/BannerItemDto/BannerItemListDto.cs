using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.BannerItemDto
{
    [AutoMap(typeof(BannerItem))]
    public class BannerItemListDto
    {
        public int Id { get; set; }
        public int BannerId { get; set; }
        public Banner Banner { get; set; }
        public string? BannerName { get { return Banner?.Name; } }
        public string Title { get; set; }
        public int Order { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Status { get; set; }
        public int BannerCount{get;set;}
        [Ignore]
        public string StatusStr
        {
            get
            {
                switch (Status)
                {
                    case 0: return "Pasif";
                    case 1: return "Aktif";
                    case 99: return "Silinmiş";
                    default: return "Belirsiz";
                };
            }
        }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
