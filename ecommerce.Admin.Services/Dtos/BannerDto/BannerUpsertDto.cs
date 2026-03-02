using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.BannerDto
{
    [AutoMap(typeof(Banner), ReverseMap = true)]
    public class BannerUpsertDto
    {
        public int? Id { get; set; }
        public BannerType BannerType { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public bool AutoLoop{get;set;} 
        public long AutoStartTime{get;set;}
        public long ReplayTime{get;set;}
        public int BannerCount{get;set;}
        public bool FullSlider{get;set;}

        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
