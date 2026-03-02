using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ProductTypeDto
{
    [AutoMap(typeof(ProductType))]
    public class ProductTypeListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? BranchId { get; set; }
        public DateTime CreatedDate { get; set; }

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
                    case 99: return "Silinmiş";
                    default: return "Belirsiz";
                };
            }
        }
    }
}
