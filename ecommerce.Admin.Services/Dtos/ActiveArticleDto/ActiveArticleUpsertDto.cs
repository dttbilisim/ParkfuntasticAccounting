using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ActiveArticleDto
{
    [AutoMap(typeof(ActiveArticle), ReverseMap = true)]
    public class ActiveArticleUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
