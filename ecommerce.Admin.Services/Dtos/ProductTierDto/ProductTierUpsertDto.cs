using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.ProductTierDto
{
    [AutoMap(typeof(ProductTier), ReverseMap = true)]
    public class ProductTierUpsertDto
    {
        public int? Id { get; set; }
        public int ProductId { get; set; }
        public int TierId { get; set; }

        public EntityStatus Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; } = true;
    }
}
