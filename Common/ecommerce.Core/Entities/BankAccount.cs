using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

/// <summary>
/// Yönetim panelindeki banka hesap tanımları için entity.
/// Ödeme tarafındaki mevcut Bank yapısından bağımsızdır; sadece tanım amaçlıdır.
/// </summary>
public class BankAccount : AuditableEntity<int>
{
    /// <summary>
    /// Ödeme tarafındaki mevcut Bank kaydına opsiyonel referans.
    /// Ayrı bir tanım modülü olsa da ilişki kurulabilsin diye bırakıldı.
    /// </summary>
    public int? BankId { get; set; }

    [MaxLength(50)]
    public string? SystemCode { get; set; }

    public int? PaymentTypeId { get; set; }

    public int? CurrencyId { get; set; }

    /// <summary>PcPos transferde bu hesaba ait ödemelerin yazılacağı kasa (CashRegisters.Id).</summary>
    public int? CashRegisterId { get; set; }

    [ForeignKey(nameof(PaymentTypeId))]
    public ecommerce.Core.Entities.Accounting.PaymentType? PaymentType { get; set; }

    [Required]
    [MaxLength(50)]
    public string AccountCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string BranchName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? CardNumber { get; set; }

    [Required]
    [MaxLength(34)]
    public string Iban { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    public bool Active { get; set; } = true;

    public int BranchId { get; set; }

    [ForeignKey(nameof(BankId))]
    public Bank? Bank { get; set; }

    [ForeignKey(nameof(CurrencyId))]
    public Currency? Currency { get; set; }

    [ForeignKey(nameof(CashRegisterId))]
    public ecommerce.Core.Entities.Accounting.CashRegister? CashRegister { get; set; }

    public ICollection<BankAccountExpense> Expenses { get; set; } = new List<BankAccountExpense>();

    public ICollection<BankAccountInstallment> Installments { get; set; } = new List<BankAccountInstallment>();
}


