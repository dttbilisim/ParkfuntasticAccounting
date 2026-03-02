using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ProductCategory
{
    [AutoMap(typeof(ProductCategories), ReverseMap = true)]
    public class ProductCategoryUpsertDto
    {
        public int? Id { get; set; }

        public int CategoryId { get; set; }
        public int ProductId { get; set; }

        [Ignore]
        public List<int> Categories { get; set; }
    }
}
