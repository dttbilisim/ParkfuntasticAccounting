using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.BannerSubItemDto
{
    [AutoMap(typeof(BannerSubItem), ReverseMap = true)]
    public class BannerSubItemUpsertDto
    {
        public int? Id { get; set; }
        public int BannerItemId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsNewTab { get; set; }
        public int Order { get; set; }
        public string FileGuid { get; set; }
        public string FileName { get; set; }
        public string Root { get; set; }
        [Ignore]
        public byte[] Base64Str
        {
            get
            {
                if (File.Exists(Root))
                    return System.IO.File.ReadAllBytes(Root);
                else
                    return new byte[0];
            }
        }
        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
