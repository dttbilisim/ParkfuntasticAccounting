using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities
{
    public class Cargo : AuditableEntity<int>
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public decimal CargoOverloadPrice { get; set; }
        public string? Message { get; set; }
        public bool IsLocalStorage { get; set; }

        /// <summary>Kargo tipi: Standart (Yurtiçi, Sendeo, MNG) vs Bicops Express (kurye teslimatı). İleride depo bazlı kargo için genişletilebilir.</summary>
        public CargoType CargoType { get; set; } = CargoType.Standard;

        /// <summary>Sadece CargoType=BicopsExpress için: Tek ücretle kapsanan mesafe (km). Bu km'ye kadar Amount (tek ücret) uygulanır.</summary>
        public decimal CoveredKm { get; set; }

        /// <summary>Sadece CargoType=BicopsExpress için: Kapsanan km sonrası her km başına ek ücret (TL).</summary>
        public decimal PricePerExtraKm { get; set; }

        public virtual List<CargoProperty> CargoProperties { get; set; }



    }
}

