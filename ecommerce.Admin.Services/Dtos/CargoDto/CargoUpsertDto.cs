using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CargoDto
{
    [AutoMap(typeof(Core.Entities.Cargo), ReverseMap = true)]
    public class CargoUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }

        public decimal Amount { get; set; } = 0;
        public decimal CargoOverloadPrice { get; set; }
        public string? Message { get; set; }

        /// <summary>Kargo tipi: Standard = 0, BicopsExpress = 1 (Hızlı Kargo Bicops Express)</summary>
        public CargoType CargoType { get; set; } = CargoType.Standard;

        /// <summary>CargoType=BicopsExpress için: Tek ücretle kapsanan mesafe (km).</summary>
        public decimal CoveredKm { get; set; }

        /// <summary>CargoType=BicopsExpress için: Kapsanan km sonrası her km başı ek ücret (TL).</summary>
        public decimal PricePerExtraKm { get; set; }

        [Ignore]
        public bool StatusBool { get; set; } = true;
    }
}
