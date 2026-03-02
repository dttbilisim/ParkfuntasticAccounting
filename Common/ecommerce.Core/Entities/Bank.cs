using System.ComponentModel.DataAnnotations;
namespace ecommerce.Core.Entities;
public class Bank{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string SystemName { get; set; }
    public int BankCode { get; set; }
    public string LogoPath { get; set; }
    public bool UseCommonPaymentPage { get; set; }
    public bool DefaultBank { get; set; }
    public bool Active { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public List<BankCreditCardInstallment> Installments { get; set; } = new List<BankCreditCardInstallment>();
    public List<BankCard> CreditCards { get; set; } = new List<BankCard>();
    public List<BankParameter> Parameters { get; set; } = new List<BankParameter>();
}

