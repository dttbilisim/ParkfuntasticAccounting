using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.CargoPropertyDto
{
    [AutoMap(typeof(CargoProperty), ReverseMap = true)]
    public class CargoPropertyUpsertDto
    {
        public int? Id { get; set; }
        public string Size { get; set; }
        public decimal Price { get; set; }
        public int DesiMinValue { get; set; }
        public int DesiMaxValue { get; set; }
        
        public int CargoId { get; set; }

        [Ignore]
        public bool StatusBool { get; set; } = true;
    }
}
