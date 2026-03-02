using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities
{
    public class CargoProperty : AuditableEntity<int>
    {
        public string Size { get; set; }
        
        public int DesiMinValue { get; set; }
        public int DesiMaxValue { get; set; }
        
        public decimal Price { get; set; }
        public int CargoId { get; set; }
        

        // [ForeignKey("CargoId ")]
        // public Cargo Cargo { get; set; }
    }
}

