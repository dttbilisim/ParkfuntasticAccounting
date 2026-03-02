using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.CompanyRateDto
{
    [AutoMap(typeof(CompanyRate))]
    public class CompanyRateListDto
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? TierId { get; set; }
        public int Rate { get; set; }

        public Product? Product { get; set; }
        public Category? Category { get; set; }
        public Tier? Tier { get; set; }

        public EntityStatus Status { get; set; }

        [Ignore]
        public string StatusStr
        {
            get
            {
                switch ((int)Status)
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
