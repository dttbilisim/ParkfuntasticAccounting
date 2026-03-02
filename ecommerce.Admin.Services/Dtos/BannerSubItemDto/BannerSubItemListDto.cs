using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.BannerSubItemDto
{
    [AutoMap(typeof(BannerSubItem))]
    public class BannerSubItemListDto
    {
        public int Id { get; set; }
        public int BannerId { get; set; }      
        public string Title { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Status { get; set; }
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
    }
}
