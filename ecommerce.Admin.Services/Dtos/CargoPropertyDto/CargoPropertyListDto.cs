using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Admin.Domain.Dtos.CargoDto;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.CargoPropertyDto
{
    [AutoMap(typeof(CargoProperty))]
    public class CargoPropertyListDto
    {
        public int Id { get; set; }
        public string Size { get; set; }

        public int DesiMinValue { get; set; }
        public int DesiMaxValue { get; set; }

        public decimal Price { get; set; }

        public CargoUpsertDto Cargo { get; set; }

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
