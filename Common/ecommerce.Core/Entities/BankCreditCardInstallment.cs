using System.ComponentModel.DataAnnotations;
namespace ecommerce.Core.Entities;
public class BankCreditCardInstallment{
    [Key]
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    /// <summary>Şube bazlı (Taksit Oranları şirket/şubeye göre ayrılır).</summary>
    public int? BranchId { get; set; }
    public int Installment { get; set; }
    public decimal InstallmentRate { get; set; }
    public bool Active { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public BankCard CreditCard { get; set; }
}
