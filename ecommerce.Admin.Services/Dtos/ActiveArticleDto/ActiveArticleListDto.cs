using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.ActiveArticleDto
{
    [AutoMap(typeof(ActiveArticle))]
    public class ActiveArticleListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public EntityStatus Status { get; set; }
    }
}
