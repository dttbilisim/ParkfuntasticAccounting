using AutoMapper;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ProductCategory
{
    [AutoMap(typeof(ProductCategories))]

    public class ProductCategoryListDto
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public Category Category { get; set; }
    }
}
