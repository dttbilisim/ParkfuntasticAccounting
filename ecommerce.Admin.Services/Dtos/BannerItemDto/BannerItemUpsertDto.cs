using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.BannerItemDto
{
    [AutoMap(typeof(BannerItem), ReverseMap = true)]
    public class BannerItemUpsertDto
    {
        public int? Id { get; set; }

        public int BannerId { get; set; }

        public int Order { get; set; }
        public string FileGuid { get; set; }
        public string FileName { get; set; }
        public string ? FileNameMobile{get;set;}
        public string Root { get; set; }
        public string ? MobileImageUrl{get;set;}
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Url { get; set; }
        public bool IsNewTab { get; set; }
        public bool IsButton { get; set; }
        public bool IsVideo{get;set;}
        public string ? VideoUrl{get;set;}
        public string? ButtonName { get; set; }
        public int BannerCount{get;set;}
        public bool FullSlider{get;set;}
        [Ignore]
        public byte[] Base64Str
        {
            get
            {
               
                if (Root!=null && File.Exists(Root))
                    return System.IO.File.ReadAllBytes(Root);
                else
                    return new byte[0];
            }          
        }
        [Ignore]
        public byte[] MobileBase64Str
        {
            get
            {
                if (MobileImageUrl!=null && File.Exists(MobileImageUrl))
                    return System.IO.File.ReadAllBytes(MobileImageUrl);
                else
                    return new byte[0];
            }          
        }
        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
