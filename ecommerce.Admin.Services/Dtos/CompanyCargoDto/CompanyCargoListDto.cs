using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.CompanyCargoDto
{
    [AutoMap(typeof(CompanyCargo))]
    public class CompanyCargoListDto
    {

        public int Id { get; set; }
        public int CargoId { get; set; }
        public Core.Entities.Cargo Cargo { get; set; }
        public int MinBasketAmount { get; set; }
        public int CargoAmount { get; set; }

        public bool IsDefault { get; set; }
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
