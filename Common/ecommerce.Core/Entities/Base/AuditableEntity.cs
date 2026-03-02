
using Audit.EntityFramework;
using ecommerce.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities.Base {
    [AuditInclude]
    public abstract class AuditableEntity<T> : Entity<T>, IAuditableEntity<T>
    {
        public int Status { get; set; }

        [Required]
        [Column(Order = 101)]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [Column(Order = 102)]
        [DataType(DataType.DateTime)]
        public DateTime? ModifiedDate { get; set; }

        [Column(Order = 103)]
        [DataType(DataType.DateTime)]
        public DateTime? DeletedDate { get; set; }

        [Required]
        public int CreatedId { get; set; }

        public int? ModifiedId { get; set; }

        public int? DeletedId { get; set; }

    }
}
