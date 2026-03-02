using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities.Accounting
{
    /// <summary>
    /// Gider / Gelir vb. işlemlerin hiyerarşik tanımları.
    /// ParentId null ise Ana İşlem, dolu ise Alt İşlem olarak yorumlanır.
    /// </summary>
    public class ExpenseDefinition : AuditableEntity<int>, ITenantEntity
    {
        [Required]
        public ExpenseOperationType OperationType { get; set; } = ExpenseOperationType.Gider;

        public int BranchId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public ExpenseDefinition? Parent { get; set; }

        public ICollection<ExpenseDefinition> Children { get; set; } = new List<ExpenseDefinition>();
    }
}


