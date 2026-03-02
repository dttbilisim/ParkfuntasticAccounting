using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.BannerDto
{
    [AutoMap(typeof(Banner))]
    public class BannerListDto
    {
        public int Id { get; set; }
        public BannerType BannerType { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public bool AutoLoop{get;set;} 
        public long AutoStartTime{get;set;}
        public long ReplayTime{get;set;}
        public int BannerCount{get;set;}
        public DateTime CreatedDate { get; set; }
        public int Status { get; set; }
      
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
