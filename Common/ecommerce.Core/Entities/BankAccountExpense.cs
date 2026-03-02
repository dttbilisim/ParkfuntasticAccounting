using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Entities.Accounting;

namespace ecommerce.Core.Entities;

/// <summary>
/// Banka hesabı - gider tanımı eşleştirmeleri.
/// </summary>
public class BankAccountExpense : AuditableEntity<int>
{
    public int BankAccountId { get; set; }

    public int MainExpenseId { get; set; }

    public int? SubExpenseId { get; set; }

    [ForeignKey(nameof(BankAccountId))]
    public BankAccount BankAccount { get; set; } = null!;

    [ForeignKey(nameof(MainExpenseId))]
    public ExpenseDefinition MainExpense { get; set; } = null!;

    [ForeignKey(nameof(SubExpenseId))]
    public ExpenseDefinition? SubExpense { get; set; }
}


