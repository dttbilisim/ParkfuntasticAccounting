using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.StaticPageDto
{
    [AutoMap(typeof(StaticPage))]
    public class StaticPageListDto
    {

        public int Id { get; set; }
        public string Content { get; set; }
        public string FileName { get; set; }
        public string FileGuid { get; set; }

        public StaticPageType StaticPageType { get; set; }
        public EntityStatus Status { get; set; }

 
    }
}
