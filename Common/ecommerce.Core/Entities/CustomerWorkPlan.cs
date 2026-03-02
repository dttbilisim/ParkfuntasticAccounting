using ecommerce.Core.Entities.Accounting;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities
{
    public class CustomerWorkPlan
    {
        public int Id { get; set; }

        public int SalesPersonId { get; set; }
        [ForeignKey(nameof(SalesPersonId))]
        public SalesPerson SalesPerson { get; set; } = null!;

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        public int DayOfWeek { get; set; } // 0=Pazar, 1=Pazartesi, ..., 6=Cumartesi

        public int MonthId { get; set; }
        [ForeignKey(nameof(MonthId))]
        public Month Month { get; set; } = null!;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}


