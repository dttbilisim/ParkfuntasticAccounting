using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.TierDto;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.ProductTierDto
{
    [AutoMap(typeof(ProductTier))]
    public class ProductTierListDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int TierId { get; set; }
        public ProductUpsertDto Product { get; set; } = default!;
        public TierUpsertDto Tier { get; set; } = default!;


        public int Status { get; set; }

        [Ignore]
        public string StatusStr
        {
            get
            {
                switch (Status)
                {
                    case 0: return "Pasif";
                    case 1: return "Aktif";
                    case 99: return "Silinmi?";
                    default: return "Belirsiz";
                };
            }
        }
    }
}
