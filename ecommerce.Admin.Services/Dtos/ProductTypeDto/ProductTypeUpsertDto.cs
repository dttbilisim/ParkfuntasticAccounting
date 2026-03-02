using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ProductTypeDto
{
    [AutoMap(typeof(ProductType), ReverseMap = true)]
    public class ProductTypeUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public int? BranchId { get; set; }
        public int Status { get; set; }


        [Ignore]
        public bool StatusBool { get; set; }
    }
}
