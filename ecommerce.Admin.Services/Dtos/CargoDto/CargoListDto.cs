using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CargoDto
{
    [AutoMap(typeof(Core.Entities.Cargo))]
    public class CargoListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public CargoType CargoType { get; set; }

        public EntityStatus Status { get; set; }

        [Ignore]
        public string CargoTypeStr
        {
            get
            {
                return CargoType switch
                {
                    CargoType.Standard => "Standart Kargo",
                    CargoType.BicopsExpress => "Hızlı Kargo Bicops Express",
                    _ => "Belirsiz"
                };
            }
        }

        [Ignore]
        public string StatusStr
        {
            get
            {
                switch ((int)Status)
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
