using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.BrandDto
{
    [AutoMap(typeof(Brand))]
    public class BrandListDto
    {
        public int Id { get; set; }

        public string IdStr
        {
            get
            {
                return Id.ToString();
            }
        }
        public string Name { get; set; }
        public int? BranchId { get; set; }
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
