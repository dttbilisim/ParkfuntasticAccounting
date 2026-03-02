using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.StaticPageDto
{
    [AutoMap(typeof(StaticPage), ReverseMap = true)]
    public class StaticPageUpsertDto
    {
        public int? Id { get; set; }
        public StaticPageType StaticPageType { get; set; }
        public string Content { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Root { get; set; }
        public string FileName { get; set; }
        public string FileGuid { get; set; }

        public EntityStatus Status { get; set; }

        [Ignore]
        public byte[] Base64Str
        {
            get
            {
                if (File.Exists(FileGuid))
                    return System.IO.File.ReadAllBytes(FileGuid);
                else
                    return new byte[0];
            }
        }

        [Ignore]
        public bool StatusBool { get; set; } = true;
    }
}
