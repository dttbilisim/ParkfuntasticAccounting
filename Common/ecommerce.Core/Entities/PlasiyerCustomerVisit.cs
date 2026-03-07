using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Accounting;

namespace ecommerce.Core.Entities
{
    /// <summary>
    /// Plasiyer müşteri ziyaret kaydı — rota listesinde ziyaret notları için.
    /// </summary>
    public class PlasiyerCustomerVisit
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        public int SalesPersonId { get; set; }
        [ForeignKey(nameof(SalesPersonId))]
        public SalesPerson SalesPerson { get; set; } = null!;

        public DateTime VisitDate { get; set; }

        [MaxLength(2000)]
        public string? VisitNote { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
