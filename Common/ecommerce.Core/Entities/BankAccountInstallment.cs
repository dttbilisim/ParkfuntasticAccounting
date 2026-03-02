using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

/// <summary>
/// Banka hesabına özel taksit seçenekleri.
/// </summary>
public class BankAccountInstallment : AuditableEntity<int>
{
    public int BankAccountId { get; set; }

    public int Installment { get; set; }

    public decimal CommissionRate { get; set; }

    public decimal Amount { get; set; }

    [Required]
    [MaxLength(250)]
    public string Note { get; set; } = string.Empty;

    public bool Active { get; set; } = true;

    [ForeignKey(nameof(BankAccountId))]
    public BankAccount BankAccount { get; set; } = null!;
}


