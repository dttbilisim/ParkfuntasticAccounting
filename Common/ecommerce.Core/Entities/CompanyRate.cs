using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities
{
    public class CompanyRate : AuditableEntity<int> {

        public int? CompanyId { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? TierId { get; set; }
        public int Rate { get; set; }



        [ForeignKey("CompanyId")]
        public Company Company { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category  { get; set; }
        [ForeignKey("TierId")]
        public Tier? Tier { get; set; }


    }
}

